using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models;
using Utility;
using DataAccess.Utility;

namespace DataAccess.Repository
{
    public class AutoBidRepository : Repository<AutoBid>, IAutoBidRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IBidRepository _bidRepository;

        public AutoBidRepository(ApplicationDbContext context, IBidRepository bidRepository) : base(context)
        {
            _context = context;
            _bidRepository = bidRepository;
        }

        public async Task<AutoBid> GetActiveAutoBidForUserAsync(int auctionId, string userId)
        {
            return await _context.AutoBids
                .FirstOrDefaultAsync(ab => ab.AuctionId == auctionId &&
                                          ab.UserId == userId &&
                                          ab.IsActive);
        }

        public async Task DeactivateAsync(int autoBidId)
        {
            var autoBid = await _context.AutoBids.FindAsync(autoBidId);
            if (autoBid != null)
            {
                autoBid.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Resolves all active auto-bids for an auction using a single-pass in-memory algorithm.
        ///
        /// Algorithm steps:
        ///   1. Load ALL active auto-bids in ONE database query (sorted by MaxAmount desc, CreatedAt asc)
        ///   2. Identify the best challenger (highest MaxAmount, not the current highest bidder)
        ///   3. Compare challenger vs current holder's auto-bid (if any) — determine winner in memory
        ///   4. Deactivate exhausted auto-bids in bulk with a single notification pass
        ///   5. Place ONE winning bid and call SaveChangesAsync ONCE
        ///
        /// This replaces the previous while-loop approach that re-queried the database on every
        /// iteration and called SaveChangesAsync multiple times, risking partial saves on failure.
        /// </summary>
        public async Task ProcessAutoBidsAsync(int auctionId, decimal currentHighestBid, string currentHighestBidUserId)
        {
            // Step 1: Load ALL active auto-bids in ONE query (sorted best first)
            var allAutoBids = await _context.AutoBids
                .Where(ab => ab.AuctionId == auctionId && ab.IsActive)
                .OrderByDescending(ab => ab.MaxAmount)
                .ThenBy(ab => ab.CreatedAt)
                .ToListAsync();

            if (!allAutoBids.Any()) return;

            var inc = BiddingEngine.CalculateIncrement(currentHighestBid);
            var minRequired = currentHighestBid + inc;

            // Step 2: Find challengers — active auto-bids from users who are NOT
            // the current highest bidder AND whose max can meet the minimum required bid
            var challengers = allAutoBids
                .Where(ab => ab.UserId != currentHighestBidUserId
                          && ab.MaxAmount >= minRequired)
                .ToList();

            // No valid challengers — deactivate any exhausted auto-bids and exit
            if (!challengers.Any())
            {
                var exhausted = allAutoBids
                    .Where(ab => ab.MaxAmount < minRequired)
                    .ToList();
                await NotifyAndDeactivateAsync(exhausted, auctionId);
                await _context.SaveChangesAsync();
                return;
            }

            // Step 3: Best challenger = first in list (highest MaxAmount, earliest if tied)
            var bestChallenger = challengers.First();

            // Does the current highest bidder also have an active auto-bid?
            var currentHolderAutoBid = allAutoBids
                .FirstOrDefault(ab => ab.UserId == currentHighestBidUserId);

            string winnerId;
            decimal winningAmount;

            if (currentHolderAutoBid != null && currentHolderAutoBid.MaxAmount >= minRequired)
            {
                // Both challenger and current holder have auto-bids — compare their maximums
                if (bestChallenger.MaxAmount > currentHolderAutoBid.MaxAmount)
                {
                    // Challenger has higher max — challenger wins
                    // Winning bid = just above current holder's max (proxy bidding rule)
                    winnerId = bestChallenger.UserId;
                    winningAmount = Math.Min(bestChallenger.MaxAmount, currentHolderAutoBid.MaxAmount + inc);
                    currentHolderAutoBid.IsActive = false; // current holder is now exhausted
                }
                else
                {
                    // Current holder has higher or equal max — holder stays on top
                    // Winning bid = just above challenger's max (proxy bidding rule)
                    winnerId = currentHolderAutoBid.UserId;
                    winningAmount = Math.Min(currentHolderAutoBid.MaxAmount, bestChallenger.MaxAmount + inc);
                    bestChallenger.IsActive = false; // challenger is now exhausted
                }
            }
            else
            {
                // Current holder has no auto-bid (or theirs is exhausted) — challenger takes the lead
                winnerId = bestChallenger.UserId;
                winningAmount = Math.Min(bestChallenger.MaxAmount, currentHighestBid + inc);
            }

            // Step 4: Deactivate ALL auto-bids whose max is now below the winning amount
            var toDeactivate = allAutoBids
                .Where(ab => ab.IsActive && ab.MaxAmount < winningAmount)
                .ToList();
            await NotifyAndDeactivateAsync(toDeactivate, auctionId);

            // Step 5: Place ONE winning bid
            await _bidRepository.AddAsync(new Bid
            {
                AuctionId = auctionId,
                UserId    = winnerId,
                Amount    = winningAmount,
                BidTime   = DateTime.UtcNow
            });

            // Single SaveChangesAsync — all changes committed atomically
            await _context.SaveChangesAsync();

            // Check if this bid triggered an anti-sniping time extension
            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction != null)
            {
                AuctionTimeExtension.ExtendIfNeeded(auction);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Marks a list of auto-bids as inactive and sends an outbid notification to each user.
        /// Called in bulk to avoid repeated DB fetches for auction/item info.
        /// Note: Does NOT call SaveChangesAsync — the caller is responsible for saving.
        /// </summary>
        private async Task NotifyAndDeactivateAsync(List<AutoBid> autoBids, int auctionId)
        {
            if (!autoBids.Any()) return;

            // Fetch auction info once for all notifications
            var auctionInfo = await _context.Auctions
                .Include(a => a.Item)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            foreach (var ab in autoBids)
            {
                ab.IsActive = false;
                _context.Notifications.Add(new Notification
                {
                    UserId          = ab.UserId,
                    Message         = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — you've been outbid.",
                    RelatedItemId   = auctionInfo?.ItemId,
                    RelatedItemName = auctionInfo?.Item?.Name
                });
            }
        }
        public async Task<bool> ExistsActiveAutoBidWithMaxAsync(int auctionId, decimal maxAmount, string excludeUserId)
        {
            return await _context.AutoBids
                .AnyAsync(ab =>
                    ab.AuctionId == auctionId &&
                    ab.IsActive &&
                    ab.MaxAmount == maxAmount &&
                    ab.UserId != excludeUserId
                );
        }
        }
}