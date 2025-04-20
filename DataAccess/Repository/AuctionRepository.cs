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

        public AuctionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Already existing methods...

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

        // ✅ New methods used in Program.cs

        public async Task StartAuctionAsync()
        {
            var now = DateTime.UtcNow;
            var auctionsToStart = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.Approved && a.StartTime <= now)
                .ToListAsync();

            foreach (var auction in auctionsToStart)
            {
                auction.Status = AuctionStatus.InAuction;
            }

            await _context.SaveChangesAsync();
        }

        public async Task EndAuctionAsync()
        {
            var now = DateTime.UtcNow;
            var auctionsToEnd = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.InAuction && a.EndTime <= now)
                .ToListAsync();

            foreach (var auction in auctionsToEnd)
            {
                auction.Status = AuctionStatus.Sold;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RelistUnsoldItemsAsync()
        {
            var now = DateTime.UtcNow;
            var unsoldAuctions = await _context.Auctions
                .Include(a => a.Bids)
                .Where(a => a.Status == AuctionStatus.Sold && !a.Bids.Any())
                .ToListAsync();

            foreach (var auction in unsoldAuctions)
            {
                var newAuction = new Auction
                {
                    ItemId = auction.ItemId,
                    Status = AuctionStatus.Approved,
                    StartTime = now.AddDays(5), // Optional: use schedule here
                    EndTime = now.AddDays(6)
                };

                await _context.Auctions.AddAsync(newAuction);
            }

            await _context.SaveChangesAsync();
        }
    }

}