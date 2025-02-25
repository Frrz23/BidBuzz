using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;


namespace Models.Models
{
    public class AuctionScheduleConfig
    {
        public int StartDay { get; set; }
        public int StartHour { get; set; }
        public int EndDay { get; set; }
        public int EndHour { get; set; }
        public int RelistHour { get; set; }

        public AuctionScheduleConfig(IConfiguration configuration)
        {
            configuration.GetSection("AuctionSchedule").Bind(this);
        }
    }
}
