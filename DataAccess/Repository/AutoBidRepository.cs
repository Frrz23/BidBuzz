using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models;

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

        public async Task ProcessAutoBidsAsync(int auctionId, decimal currentHighestBid, string currentHighestBidUserId)
        {
            // Get all active auto bids for this auction except the one from the current highest bidder
            var autoBids = await _context.AutoBids
                .Where(ab => ab.AuctionId == auctionId &&
                           ab.IsActive &&
                           ab.UserId != currentHighestBidUserId &&
                           ab.MaxAmount > currentHighestBid)
                .OrderByDescending(ab => ab.MaxAmount)
                .ThenBy(ab => ab.CreatedAt)
                .ToListAsync();

            if (!autoBids.Any())
                return;

            // Get the auto bid with the highest max amount
            var highestAutoBid = autoBids.First();

            // If the current highest bidder also has an auto bid, we need to handle the outbidding logic
            var currentBidderAutoBid = await GetActiveAutoBidForUserAsync(auctionId, currentHighestBidUserId);

            decimal bidAmount;

            if (currentBidderAutoBid != null)
            {
                // If current highest bidder has auto bid, we need to see who has the higher max amount
                if (highestAutoBid.MaxAmount > currentBidderAutoBid.MaxAmount)
                {
                    // The challenger will bid just above the current bidder's max
                    bidAmount = Math.Min(highestAutoBid.MaxAmount, currentBidderAutoBid.MaxAmount + 1);

                    // Deactivate the current bidder's auto bid as it's been outbid
                    await DeactivateAsync(currentBidderAutoBid.Id);
                }
                else
                {
                    // Current bidder has higher max, so we'll just bump up to outbid the challenger
                    return; // No need to do anything as current bidder remains highest
                }
            }
            else
            {
                // Normal case: just outbid the current highest by the minimum increment
                bidAmount = currentHighestBid + 1; // Minimum increment of $1
            }

            // Create and place the new bid
            var bid = new Bid
            {
                AuctionId = auctionId,
                UserId = highestAutoBid.UserId,
                Amount = bidAmount,
                BidTime = DateTime.UtcNow
            };

            await _bidRepository.AddAsync(bid);

            // If the bid amount reaches max amount, deactivate the auto bid
            if (bidAmount >= highestAutoBid.MaxAmount)
            {
                await DeactivateAsync(highestAutoBid.Id);
            }
        }
    }
}