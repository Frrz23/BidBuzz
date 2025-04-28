using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Models;
using Utility;

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
            var increment = BiddingDefaults.Increment;

            // 1) find challengers who can actually outbid
            var autoBids = await _context.AutoBids
                .Where(ab => ab.AuctionId == auctionId
                          && ab.IsActive
                          && ab.UserId != currentHighestBidUserId
                          && ab.MaxAmount >= currentHighestBid + increment)
                .OrderByDescending(ab => ab.MaxAmount)
                .ThenBy(ab => ab.CreatedAt)
                .ToListAsync();

            if (autoBids.Any())
            {
                var highestAutoBid = autoBids.First();
                var currentBidderAutoBid = await GetActiveAutoBidForUserAsync(auctionId, currentHighestBidUserId);

                decimal bidAmount;
                if (currentBidderAutoBid != null)
                {
                    if (highestAutoBid.MaxAmount > currentBidderAutoBid.MaxAmount)
                    {
                        // bid one increment above *their* cap, capped at your own cap
                        bidAmount = Math.Min(
                            highestAutoBid.MaxAmount,
                            currentBidderAutoBid.MaxAmount + increment
                        );

                        await DeactivateAsync(currentBidderAutoBid.Id);
                    }
                    else
                    {
                        // current stays highest—nothing to do
                        bidAmount = 0;
                    }
                }
                else
                {
                    // outbid the top, capped at your max
                    bidAmount = Math.Min(
                        highestAutoBid.MaxAmount,
                        currentHighestBid + increment
                    );
                }

                if (bidAmount > 0)
                {
                    await _bidRepository.AddAsync(new Bid
                    {
                        AuctionId = auctionId,
                        UserId = highestAutoBid.UserId,
                        Amount = bidAmount,
                        BidTime = DateTime.UtcNow
                    });

                    if (bidAmount >= highestAutoBid.MaxAmount)
                        await DeactivateAsync(highestAutoBid.Id);
                }
            }

            // 2) deactivate *all* exhausted auto bids so none linger forever
            var exhausted = await _context.AutoBids
                .Where(ab => ab.AuctionId == auctionId
                          && ab.IsActive
                          && ab.MaxAmount <= currentHighestBid)
                .ToListAsync();
            foreach (var ab in exhausted)
                ab.IsActive = false;

            if (exhausted.Any())
                await _context.SaveChangesAsync();
        }

    }
}