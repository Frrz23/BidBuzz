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
            int daysUntilStart = ((startDay - (int)now.DayOfWeek + 7) % 7);
            var nextAuctionStart = now.Date.AddDays(daysUntilStart);
            return new DateTime(nextAuctionStart.Year, nextAuctionStart.Month, nextAuctionStart.Day, startHour, 0, 0);
        }

        public static DateTime GetNextAuctionEnd(int endDay, int endHour)
        {
            var now = DateTime.UtcNow;
            int daysUntilEnd = ((endDay - (int)now.DayOfWeek + 7) % 7);
            var endAuctionDate = now.Date.AddDays(daysUntilEnd);
            return new DateTime(endAuctionDate.Year, endAuctionDate.Month, endAuctionDate.Day, endHour, 0, 0);
        }
    }

}
