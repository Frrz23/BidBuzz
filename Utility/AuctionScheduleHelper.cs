using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class AuctionScheduleHelper
    {
        public static DateTime GetNextAuctionStart(int startDay, int startHour)
        {
            var now = DateTime.UtcNow;
            var nextAuctionStart = now.AddDays((startDay - (int)now.DayOfWeek + 7) % 7);
            return new DateTime(nextAuctionStart.Year, nextAuctionStart.Month, nextAuctionStart.Day, startHour, 0, 0);
        }

        public static DateTime GetNextAuctionEnd(int endDay, int endHour)
        {
            var start = GetNextAuctionStart(endDay, endHour);
            return new DateTime(start.Year, start.Month, start.Day, endHour, 0, 0);
        }
    }
}
