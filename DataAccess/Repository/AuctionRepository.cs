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
        //private DateTime GetDateTimeForDayAndHour(string dayOfWeek, int hour, DateTime reference)
        //{
        //    // Get the DayOfWeek enum from string
        //    var targetDay = Enum.Parse<DayOfWeek>(dayOfWeek);

        //    // Start from the beginning of this week (Sunday) and add days
        //    int daysUntilTarget = ((int)targetDay - (int)reference.DayOfWeek + 7) % 7;
        //    var targetDate = reference.Date.AddDays(daysUntilTarget).AddHours(hour);

        //    return targetDate;
        //}


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
                Console.WriteLine($"Auction {auction.Id} ended as {auction.Status}");

            }

            await _context.SaveChangesAsync();
        }


        public async Task RelistUnsoldItemsAsync()
        {
            // 1) Pull down *all* auctions that ended unsold - include the Item relationship
            var unsoldAuctions = await _context.Auctions
                .Include(a => a.Item)
                .Where(a => a.Status == AuctionStatus.Unsold)
                .ToListAsync();

            foreach (var auction in unsoldAuctions)
            {
                // Increment the relist count for tracking purposes
                auction.RelistCount = (auction.RelistCount ?? 0) + 1;

                // Check if this auction has been relisted too many times
                if (auction.RelistCount >= 3)
                {
                    Console.WriteLine($"Removing Auction {auction.Id} and Item {auction.Item?.Id} at RelistCount {auction.RelistCount}");

                    // Remove the associated item first (if it exists)
                    if (auction.Item != null)
                    {
                        _context.Items.Remove(auction.Item);
                    }

                    // Remove the auction that has been relisted 3 or more times
                    _context.Auctions.Remove(auction);
                }
                else
                {
                    // Reset to Unsold so it can be processed again in the next cycle
                    auction.Status = AuctionStatus.Unsold;
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
        }

    }

}