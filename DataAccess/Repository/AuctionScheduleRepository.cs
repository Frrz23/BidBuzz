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
    public class AuctionScheduleRepository : Repository<AuctionSchedule>, IAuctionScheduleRepository
    {
        private readonly ApplicationDbContext _context;

        public AuctionScheduleRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AuctionSchedule> GetScheduleAsync(string week)
        {
            return await _context.AuctionSchedules.FirstOrDefaultAsync(s => s.Week == week);
        }

        public async Task<AuctionSchedule?> GetCurrentScheduleAsync()
        {
            return await _context.AuctionSchedules.FirstOrDefaultAsync(s => s.Week == "Current");
        }

        public async Task AddScheduleAsync(AuctionSchedule schedule)
        {
            await _context.AuctionSchedules.AddAsync(schedule);
        }

        public async Task UpdateScheduleAsync(AuctionSchedule schedule)
        {
            _context.AuctionSchedules.Update(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task SeedInitialScheduleAsync()
        {
            if (!_context.AuctionSchedules.Any())
            {
                var initialSchedule = new AuctionSchedule
                {
                    Week = "Current",
                    StartDay = "Saturday",
                    StartHour = 12,
                    EndDay = "Sunday",
                    EndHour = 1
                };

                await _context.AuctionSchedules.AddAsync(initialSchedule);
                await _context.SaveChangesAsync();
            }
        }
        public async Task RotateAndPrepareNextScheduleAsync()
        {
            var current = await _context.AuctionSchedules.FirstOrDefaultAsync(s => s.Week == "Current");
            var next = await _context.AuctionSchedules.FirstOrDefaultAsync(s => s.Week == "Next");

            if (current != null)
                _context.AuctionSchedules.Remove(current); // Optional: Or mark as "Old"

            if (next != null)
            {
                next.Week = "Current";

                // Create a clone of this as the new "Next"
                var clonedNext = new AuctionSchedule
                {
                    Week = "Next",
                    StartDay = next.StartDay,
                    StartHour = next.StartHour,
                    EndDay = next.EndDay,
                    EndHour = next.EndHour
                };

                await _context.AuctionSchedules.AddAsync(clonedNext);
            }

            await _context.SaveChangesAsync();
        }

    }

}
