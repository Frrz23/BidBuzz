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

        public async Task ProcessAutoBidsAsync(int auctionId, decimal currentHighestBid, string currentHighestBidUserId)
        {

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

                if (exhausted.Any())
                {
                    var auctionInfo = await _context.Auctions
                        .Include(a => a.Item)
                        .FirstOrDefaultAsync(a => a.Id == auctionId);

                    foreach (var ex in exhausted)
                    {
                        ex.IsActive = false;
                        _context.Notifications.Add(new Notification
                        {
                            UserId = ex.UserId,
                            Message = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — you've been outbid.",
                            RelatedItemId = auctionInfo?.ItemId,
                            RelatedItemName = auctionInfo?.Item?.Name
                        });
                    }
                    await _context.SaveChangesAsync();
                }

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
                var inc = BiddingEngine.CalculateIncrement(currentHighestBid);
                var minRequired = currentHighestBid + inc;

                if (currentAutoBid != null)
                {
                    if (challenger.MaxAmount > currentAutoBid.MaxAmount)
                    {
                        if (challenger.MaxAmount < minRequired)
                        {
                            challenger.IsActive = false;
                            var auctionInfo = await _context.Auctions
                                .Include(a => a.Item)
                                .FirstOrDefaultAsync(a => a.Id == auctionId);
                            _context.Notifications.Add(new Notification
                            {
                                UserId = challenger.UserId,
                                Message = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — your maximum is below the required minimum.",
                                RelatedItemId = auctionInfo?.ItemId,
                                RelatedItemName = auctionInfo?.Item?.Name
                            });
                            await _context.SaveChangesAsync();
                            continue;
                        }
                        var nextBidAmount = Math.Min(challenger.MaxAmount, currentAutoBid.MaxAmount + inc);
                        var nextHighestUserId = challenger.UserId;
                        if (currentAutoBid.MaxAmount < nextBidAmount)
                            currentAutoBid.IsActive = false;

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
                    }
                    else
                    {
                        if (currentAutoBid.MaxAmount < minRequired)
                        {
                            challenger.IsActive = false;
                            currentAutoBid.IsActive = false;
                            var auctionInfo = await _context.Auctions
                                .Include(a => a.Item)
                                .FirstOrDefaultAsync(a => a.Id == auctionId);
                            _context.Notifications.Add(new Notification
                            {
                                UserId = challenger.UserId,
                                Message = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — your maximum is below the required minimum.",
                                RelatedItemId = auctionInfo?.ItemId,
                                RelatedItemName = auctionInfo?.Item?.Name
                            });
                            _context.Notifications.Add(new Notification
                            {
                                UserId = currentAutoBid.UserId,
                                Message = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — your maximum is below the required minimum.",
                                RelatedItemId = auctionInfo?.ItemId,
                                RelatedItemName = auctionInfo?.Item?.Name
                            });
                            await _context.SaveChangesAsync();
                            break;
                        }
                        var nextBidAmount = Math.Min(currentAutoBid.MaxAmount, challenger.MaxAmount + inc);
                        var nextHighestUserId = currentAutoBid.UserId;
                        if (challenger.MaxAmount < nextBidAmount)
                            challenger.IsActive = false;

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
                    }
                }
                else
                {
                    if (challenger.MaxAmount < minRequired)
                    {
                        challenger.IsActive = false;
                        var auctionInfo = await _context.Auctions
                            .Include(a => a.Item)
                            .FirstOrDefaultAsync(a => a.Id == auctionId);
                        _context.Notifications.Add(new Notification
                        {
                            UserId = challenger.UserId,
                            Message = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — your maximum is below the required minimum.",
                            RelatedItemId = auctionInfo?.ItemId,
                            RelatedItemName = auctionInfo?.Item?.Name
                        });
                        await _context.SaveChangesAsync();
                        break;
                    }
                    var nextBidAmount = Math.Min(challenger.MaxAmount, currentHighestBid + inc);
                    var nextHighestUserId = challenger.UserId;

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
                }

                var exhausted = await _context.AutoBids
                    .Where(ab => ab.AuctionId == auctionId
                              && ab.IsActive
                              && ab.MaxAmount <= currentHighestBid)
                    .ToListAsync();

                if (exhausted.Any())
                {
                    var auctionInfo = await _context.Auctions
                        .Include(a => a.Item)
                        .FirstOrDefaultAsync(a => a.Id == auctionId);

                    foreach (var ex in exhausted)
                    {
                        ex.IsActive = false;
                        _context.Notifications.Add(new Notification
                        {
                            UserId = ex.UserId,
                            Message = $"Your auto-bid on \"{auctionInfo?.Item?.Name ?? "an item"}\" has been stopped — you've been outbid.",
                            RelatedItemId = auctionInfo?.ItemId,
                            RelatedItemName = auctionInfo?.Item?.Name
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }

            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction != null)
            {
                AuctionTimeExtension.ExtendIfNeeded(auction);
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