using System;
using Models;

namespace DataAccess.Utility
{
    public static class AuctionTimeExtension
    {
        private static readonly TimeSpan Threshold = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ExtensionAmount = TimeSpan.FromMinutes(2);
        private const int MaxExtensions = 3;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

        public static DateTime? ExtendIfNeeded(Auction auction)
        {
            if (auction.EndTime == null) return null;

            var now = DateTime.UtcNow;
            var timeRemaining = auction.EndTime.Value - now;

            var timeSinceLastExtension = auction.LastExtensionTime != null
                ? now - auction.LastExtensionTime.Value
                : TimeSpan.MaxValue;

            if (timeRemaining > TimeSpan.Zero
                && timeRemaining <= Threshold
                && auction.ExtensionCount < MaxExtensions
                && timeSinceLastExtension >= Cooldown)
            {
                auction.EndTime = auction.EndTime.Value.Add(ExtensionAmount);
                auction.ExtensionCount++;
                auction.LastExtensionTime = now;
                return auction.EndTime;
            }

            return null;
        }
    }
}
