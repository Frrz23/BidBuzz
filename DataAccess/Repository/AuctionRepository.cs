using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repository
{
    public class AuctionRepository : Repository<Auction>, IAuctionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuctionScheduleRepository _scheduleRepo;

        public AuctionRepository(ApplicationDbContext context, IAuctionScheduleRepository scheduleRepo) : base(context)
        {
            _context = context;
            _scheduleRepo = scheduleRepo;
        }

        public async Task<List<Auction>> GetAuctionsByStatusAsync(AuctionStatus status)
        {
            return await _context.Auctions
                .Include(a => a.Item)
                .Include(a => a.Bids)
                .Where(a => a.Status == status)
                .ToListAsync();
        }

        public async Task<Auction?> GetAuctionWithHighestBidAsync(int itemId)
        {
            return await _context.Auctions
                .Include(a => a.Bids)
                .Where(a => a.ItemId == itemId)
                .OrderByDescending(a => a.Bids.Max(b => b.Amount))
                .FirstOrDefaultAsync();
        }

        public async Task CancelAuctionAsync(int auctionId)
        {
            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction != null && auction.Status == AuctionStatus.InAuction)
            {
                auction.Status = AuctionStatus.Cancelled;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Auction?> GetAuctionByItemIdAsync(int itemId)
        {
            return await _context.Auctions.FirstOrDefaultAsync(a => a.ItemId == itemId);
        }

        public async Task<List<Auction>> GetCancelledAuctionsAsync()
        {
            return await _context.Auctions
                .Include(a => a.Item)
                .Include(a => a.Bids)
                .Where(a => a.Status == AuctionStatus.Cancelled)
                .ToListAsync();
        }

        public async Task StartAuctionAsync()
        {
            var toStart = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.Approved)
                .ToListAsync();

            if (!toStart.Any()) return;

            var nowUtc = DateTime.UtcNow;
            foreach (var auction in toStart)
            {
                auction.StartTime = nowUtc;
                auction.Status = AuctionStatus.InAuction;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Ends all active auctions and determines the winner using a Winner Determination algorithm.
        ///
        /// Algorithm steps per auction:
        ///   1. Collect all bids for the auction
        ///   2. Find the highest bid amount
        ///   3. Tie-breaking rule: if two bids share the same highest amount,
        ///      the EARLIER bid wins (first-come, first-served — standard fairness rule)
        ///   4. Record the winner's UserId on the auction (WinnerId field)
        ///   5. Send a congratulatory notification to the winner
        ///   6. If no bids at all, mark auction as Unsold for relisting
        /// </summary>
        public async Task EndAuctionAsync()
        {
            var auctionsToEnd = await _context.Auctions
                .Include(a => a.Bids)
                .Include(a => a.Item)
                .Where(a => a.Status == AuctionStatus.InAuction)
                .ToListAsync();

            var now = DateTime.UtcNow;

            foreach (var auction in auctionsToEnd)
            {
                auction.EndTime = now;

                if (auction.Bids.Any())
                {
                    // Step 1: Find the maximum bid amount
                    var maxAmount = auction.Bids.Max(b => b.Amount);

                    // Step 2: Tie-breaking — if multiple bids share the same max,
                    // the one placed EARLIEST wins (first-come, first-served)
                    var winningBid = auction.Bids
                        .Where(b => b.Amount == maxAmount)
                        .OrderBy(b => b.BidTime)
                        .First();

                    // Step 3: Mark auction as Sold and store the winner
                    auction.Status        = AuctionStatus.Sold;
                    auction.WinnerId      = winningBid.UserId;
                    auction.PaymentStatus = PaymentStatus.ToPay;

                    // Step 4: Send a winner notification
                    _context.Notifications.Add(new Notification
                    {
                        UserId          = winningBid.UserId,
                        Message         = $"Congratulations! You won the auction for \"{auction.Item?.Name ?? "an item"}\" with a bid of {winningBid.Amount:C}.",
                        RelatedItemId   = auction.ItemId,
                        RelatedItemName = auction.Item?.Name
                    });

                    Console.WriteLine($"Auction {auction.Id} ended — Winner: {winningBid.UserId}, Amount: {winningBid.Amount:C}");
                }
                else
                {
                    auction.Status = AuctionStatus.Unsold;
                    Console.WriteLine($"Auction {auction.Id} ended as Unsold — no bids received.");
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RelistUnsoldItemsAsync()
        {
            var unsoldAuctions = await _context.Auctions
                .Include(a => a.Item)
                .Where(a => a.Status == AuctionStatus.Unsold)
                .ToListAsync();

            foreach (var auction in unsoldAuctions)
            {
                auction.RelistCount = (auction.RelistCount ?? 0) + 1;

                if (auction.RelistCount >= 3)
                {
                    Console.WriteLine($"Removing Auction {auction.Id} and Item {auction.Item?.Id} at RelistCount {auction.RelistCount}");

                    if (auction.Item != null)
                    {
                        _context.Items.Remove(auction.Item);
                    }

                    _context.Auctions.Remove(auction);
                }
                else
                {
                    auction.Status = AuctionStatus.Unsold;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}