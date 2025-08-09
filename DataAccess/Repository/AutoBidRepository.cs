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

            
            var autoBids = await _context.AutoBids
                .Where(ab => ab.AuctionId == auctionId
                          && ab.IsActive
                          && ab.UserId != currentHighestBidUserId
                          && ab.MaxAmount > currentHighestBid)
                .OrderByDescending(ab => ab.MaxAmount)
                .ThenBy(ab => ab.CreatedAt)
                .ToListAsync();

            if (!autoBids.Any())
            {
                
                var exhausted = await _context.AutoBids
                    .Where(ab => ab.AuctionId == auctionId
                              && ab.IsActive
                              && ab.MaxAmount <= currentHighestBid)
                    .ToListAsync();

                foreach (var ex in exhausted)
                    ex.IsActive = false;

                if (exhausted.Any())
                    await _context.SaveChangesAsync();

                return;
            }

            
            bool continueBidding = true;
            while (continueBidding)
            {
                
                autoBids = await _context.AutoBids
                    .Where(ab => ab.AuctionId == auctionId
                              && ab.IsActive
                              && ab.UserId != currentHighestBidUserId
                              && ab.MaxAmount > currentHighestBid)
                    .OrderByDescending(ab => ab.MaxAmount)
                    .ThenBy(ab => ab.CreatedAt)
                    .ToListAsync();

                if (!autoBids.Any())
                    break;

                var challenger = autoBids.First();
                var currentAutoBid = await GetActiveAutoBidForUserAsync(auctionId, currentHighestBidUserId);

                decimal nextBidAmount;
                string nextHighestUserId;

                if (currentAutoBid != null)
                {
                    
                    if (challenger.MaxAmount > currentAutoBid.MaxAmount)
                    {
                        
                        nextBidAmount = Math.Min(challenger.MaxAmount, currentAutoBid.MaxAmount + increment);
                        nextHighestUserId = challenger.UserId;

                        
                        if (currentAutoBid.MaxAmount < nextBidAmount)
                            currentAutoBid.IsActive = false;
                    }
                    else
                    {
                        
                        nextBidAmount = Math.Min(currentAutoBid.MaxAmount, challenger.MaxAmount + increment);
                        nextHighestUserId = currentAutoBid.UserId;

                        
                        if (challenger.MaxAmount < nextBidAmount)
                            challenger.IsActive = false;
                    }
                }
                else
                {
                    
                    nextBidAmount = Math.Min(challenger.MaxAmount, currentHighestBid + increment);
                    nextHighestUserId = challenger.UserId;
                }

                await _bidRepository.AddAsync(new Bid
                {
                    AuctionId = auctionId,
                    UserId = nextHighestUserId,
                    Amount = nextBidAmount,
                    BidTime = DateTime.UtcNow
                });

                currentHighestBid = nextBidAmount;
                currentHighestBidUserId = nextHighestUserId;

                await _context.SaveChangesAsync();

                var exhausted = await _context.AutoBids
                    .Where(ab => ab.AuctionId == auctionId
                              && ab.IsActive
                              && ab.MaxAmount <= currentHighestBid)
                    .ToListAsync();

                foreach (var ex in exhausted)
                    ex.IsActive = false;

                if (exhausted.Any())
                    await _context.SaveChangesAsync();
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