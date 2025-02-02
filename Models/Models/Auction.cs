using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace Models.Models
{
    public class Auction
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        // Status (Pending, Active, Completed, etc.)
        public AuctionStatus Status { get; set; }

        // Winning bid (nullable, only set after auction ends)
        public int? WinningBidId { get; set; }
        public Bid? WinningBid { get; set; }
    }

}
