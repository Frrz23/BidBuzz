using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{

    public class Bid
    {
        public int Id { get; set; }
        public int AuctionId { get; set; }
        public Auction Auction { get; set; }
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }   
        public ApplicationUser? User { get; set; }  // Navigation property

    }


}
