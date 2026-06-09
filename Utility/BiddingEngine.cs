using System;

namespace Utility
{
    public static class BiddingEngine
    {
        private const decimal MinIncrement = 1.00m;

        public static decimal CalculateIncrement(decimal currentBid)
        {
            if (currentBid < 100m)
            {
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.05m, 2));
            }
            else if (currentBid <= 500m)
            {
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.03m, 2));
            }
            else
            {
                return Math.Max(MinIncrement, Math.Round(currentBid * 0.02m, 2));
            }
        }
    }
}
