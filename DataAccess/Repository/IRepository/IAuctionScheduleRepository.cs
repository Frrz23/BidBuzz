using Models;
using Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repository.IRepository
{
    public interface IAuctionScheduleRepository : IRepository<AuctionSchedule>
    {
        Task<AuctionSchedule> GetScheduleAsync(string week);
        Task<AuctionSchedule?> GetCurrentScheduleAsync();
        Task AddScheduleAsync(AuctionSchedule schedule);
        Task UpdateScheduleAsync(AuctionSchedule schedule);
        Task SeedInitialScheduleAsync();
        Task RotateAndPrepareNextScheduleAsync();
    }


}
