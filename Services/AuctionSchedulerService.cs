using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using DataAccess.Repository.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BidBuzz.Services
{
    public class AuctionSchedulerService
    {
        private readonly IServiceProvider _services;

        public AuctionSchedulerService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Calculates the next UTC DateTime corresponding to the given target day and local hour.
        /// </summary>
        private DateTime GetNextUtcForLocal(
            DayOfWeek targetDay,
            int localHour,
            DateTime utcNow,
            bool forceNextWeek = false)
        {
            var localNow = utcNow.ToLocalTime();
            var baseLocal = new DateTime(
                localNow.Year, localNow.Month, localNow.Day,
                0, 0, 0, DateTimeKind.Local);

            // days until the _first_ occurrence
            int daysUntil = ((int)targetDay - (int)localNow.DayOfWeek + 7) % 7;

            // if we’re scheduling for “Next” after rotation,
            // push it a full extra week out
            if (forceNextWeek)
                daysUntil += 7;

            var nextLocal = baseLocal
                .AddDays(daysUntil)
                .AddHours(localHour);

            // preserve your old "if it’s already passed" check
            if (!forceNextWeek && nextLocal <= localNow)
                nextLocal = nextLocal.AddDays(7);

            return nextLocal.ToUniversalTime();
        }

        /// <summary>
        /// Schedules start and end jobs for the current auction schedule only if not already scheduled.
        /// </summary>
        public async Task ScheduleNextCycleAsync(bool forceNextWeek = false)
        {
            using var scope = _services.CreateScope();
            var schedRepo = scope.ServiceProvider.GetRequiredService<IAuctionScheduleRepository>();
            var sched = await schedRepo.GetScheduleAsync("Current");
            var nowUtc = DateTime.UtcNow;

            var startUtc = GetNextUtcForLocal(
                Enum.Parse<DayOfWeek>(sched.StartDay),
                sched.StartHour,
                nowUtc,
                forceNextWeek
            );

            var endUtc = GetNextUtcForLocal(
                Enum.Parse<DayOfWeek>(sched.EndDay),
                sched.EndHour,
                nowUtc,
                forceNextWeek
            );

            var monitor = JobStorage.Current.GetMonitoringApi();
            var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue);

            // Look for jobs in a ±1 hour window instead of exact match
            bool startExists = scheduledJobs.Any(j =>
                j.Value.Job.Method.Name == nameof(StartAuctionJob) &&
                Math.Abs((j.Value.EnqueueAt.ToUniversalTime() - startUtc).TotalMinutes) < 60);

            if (!startExists)
            {
                BackgroundJob.Schedule(() => StartAuctionJob(), startUtc);
            }

            bool endExists = scheduledJobs.Any(j =>
                j.Value.Job.Method.Name == nameof(EndAuctionJob) &&
                Math.Abs((j.Value.EnqueueAt.ToUniversalTime() - endUtc).TotalMinutes) < 60);

            if (!endExists)
            {
                BackgroundJob.Schedule(() => EndAuctionJob(), endUtc);
            }
        }



        /// <summary>
        /// Fires at the scheduled start time to transition approved auctions to InAuction.
        /// </summary>
        public async Task StartAuctionJob()
        {
            using var scope = _services.CreateScope();
            var auctionRepo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
            await auctionRepo.StartAuctionAsync();
        }

        /// <summary>
        /// Fires at the scheduled end time to end auctions, relist unsold items, rotate schedule, and schedule the next cycle.
        /// </summary>
        public void EndAuctionJob()
        {
            using var scope = _services.CreateScope();
            var auctionRepo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
            var schedRepo = scope.ServiceProvider.GetRequiredService<IAuctionScheduleRepository>();

            Task.Run(async () =>
            {
                await auctionRepo.EndAuctionAsync();
                await auctionRepo.RelistUnsoldItemsAsync();

                // ✅ rotate only AFTER auctions are ended
                await schedRepo.RotateAndPrepareNextScheduleAsync();

                // ✅ schedule next jobs for NEXT week
                await ScheduleNextCycleAsync(forceNextWeek: true);
            }).Wait();
        }

    }
}
