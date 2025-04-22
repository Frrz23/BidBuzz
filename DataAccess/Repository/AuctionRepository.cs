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

        public AuctionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
            _scheduleRepo = new AuctionScheduleRepository(context);
        }

        // Already existing methods...
        private DateTime GetDateTimeForDayAndHour(string dayOfWeek, int hour, DateTime reference)
        {
            // Get the DayOfWeek enum from string
            var targetDay = Enum.Parse<DayOfWeek>(dayOfWeek);

            // Start from the beginning of this week (Sunday) and add days
            int daysUntilTarget = ((int)targetDay - (int)reference.DayOfWeek + 7) % 7;
            var targetDate = reference.Date.AddDays(daysUntilTarget).AddHours(hour);

            return targetDate;
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

        // ✅ New methods used in Program.cs

        public async Task StartAuctionAsync()
        {
            // grab all auctions that an Admin previously Approved
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




        public async Task EndAuctionAsync()
        {
            var auctionsToEnd = await _context.Auctions
                .Include(a => a.Bids)
                .Where(a => a.Status == AuctionStatus.InAuction)
                .ToListAsync();

            var now = DateTime.UtcNow;

            foreach (var auction in auctionsToEnd)
            {
                auction.EndTime = now;
                auction.Status = auction.Bids.Any()
                    ? AuctionStatus.Sold
                    : AuctionStatus.Unsold;
            }

            await _context.SaveChangesAsync();
        }


        public async Task RelistUnsoldItemsAsync()
        {
            // 1) Pull down *all* auctions that ended unsold
            var unsoldAuctions = await _context.Auctions
                .Where(a => a.Status == AuctionStatus.Unsold)
                .ToListAsync();

            // 2) Group them by ItemId so we can count per item in one pass
            var groups = unsoldAuctions
                .GroupBy(a => a.ItemId);

            foreach (var group in groups)
            {
                var itemId = group.Key;
                var unsoldCount = group.Count();

                if (unsoldCount >= 3)
                {
                    // 3a) Too many unsold attempts: delete *all* these auctions
                    _context.Auctions.RemoveRange(group);
                }
                else
                {
                    // 3b) Otherwise, increment relist count and reset status for future relisting
                    foreach (var auction in group)
                    {
                        auction.RelistCount = (auction.RelistCount ?? 0) + 1;
                        auction.Status = AuctionStatus.Unsold;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

    }

}