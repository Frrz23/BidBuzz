using System;

namespace Utility
{
    /// <summary>
    /// Calculates the minimum bid increment based on the current bid amount.
    /// Uses a 6-tier percentage-based system for more precise and fair increment scaling.
    ///
    /// Tier breakdown:
    ///   Below 100       → 5%   (encourages quick early bidding)
    ///   100 – 499       → 4%   (moderate low-range items)
    ///   500 – 1,999     → 3%   (mid-range items)
    ///   2,000 – 9,999   → 2%   (higher value items)
    ///   10,000 – 49,999 → 1.5% (premium items)
    ///   50,000+         → 1%   (very high value, small steps)
    /// </summary>
    public static class BiddingEngine
    {
        private const decimal MinIncrement = 1.00m;

        public static decimal CalculateIncrement(decimal currentBid)
        {
            if (currentBid < 100m)
            {
                // Tier 1: 5% — encourages active early bidding
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.05m, 2));
            }
            else if (currentBid < 500m)
            {
                // Tier 2: 4% — low-range items
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.04m, 2));
            }
            else if (currentBid < 2000m)
            {
                // Tier 3: 3% — mid-range items
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.03m, 2));
            }
            else if (currentBid < 10000m)
            {
                // Tier 4: 2% — higher value items
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.02m, 2));
            }
            else if (currentBid < 50000m)
            {
                // Tier 5: 1.5% — premium items
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.015m, 2));
            }
            else
            {
                // Tier 6: 1% — very high value items (large amounts, small % steps)
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.01m, 2));
            }
        }
    }
}
