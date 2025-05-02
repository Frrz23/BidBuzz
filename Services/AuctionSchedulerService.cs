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

        public DateTime GetNextUtcForLocal(
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

            // if we’re scheduling for “Next” after rotation add 7 days

            if (forceNextWeek)
                daysUntil += 7;

            var nextLocal = baseLocal
                .AddDays(daysUntil)
                .AddHours(localHour);


            if (!forceNextWeek && nextLocal <= localNow)
                nextLocal = nextLocal.AddDays(7);

            return nextLocal.ToUniversalTime();
        }

        public async Task ScheduleNextCycleAsync(bool forceNextWeek = false)
        {
            using var scope = _services.CreateScope();
            var schedRepo = scope.ServiceProvider.GetRequiredService<IAuctionScheduleRepository>();
            var sched = await schedRepo.GetScheduleAsync("Current");
            if (sched == null) return;

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

            // If we’re bootstrapping (forceNextWeek==false) but we’re already past this week’s end, bail out
            if (!forceNextWeek && nowUtc > endUtc)
                return;

            var monitor = JobStorage.Current.GetMonitoringApi();
            var scheduledJobs = monitor.ScheduledJobs(0, int.MaxValue);

            // === NEW: only check for any pending StartAuctionJob, ignore exact time ===
            bool startExists = scheduledJobs.Any(j =>
                j.Value.Job.Method.Name == nameof(StartAuctionJob) &&
                j.Value.EnqueueAt > nowUtc);

            if (!startExists)
                BackgroundJob.Schedule(() => StartAuctionJob(), startUtc);

            // === NEW: only check for any pending EndAuctionJob ===
            bool endExists = scheduledJobs.Any(j =>
                j.Value.Job.Method.Name == nameof(EndAuctionJob) &&
                j.Value.EnqueueAt > nowUtc);

            if (!endExists)
                BackgroundJob.Schedule(() => EndAuctionJob(), endUtc);
        }

        public async Task StartAuctionJob()
        {
            using var scope = _services.CreateScope();
            var auctionRepo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
            await auctionRepo.StartAuctionAsync();
        }

        public void EndAuctionJob()
        {
            using var scope = _services.CreateScope();
            var auctionRepo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
            var schedRepo = scope.ServiceProvider.GetRequiredService<IAuctionScheduleRepository>();

            Task.Run(async () =>
            {
                await auctionRepo.EndAuctionAsync();
                await auctionRepo.RelistUnsoldItemsAsync();


                await schedRepo.RotateAndPrepareNextScheduleAsync();


                await ScheduleNextCycleAsync(forceNextWeek: true);
            }).Wait();
        }

    }
}
 