using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class Bid
    {
        public Guid Id { get; set; }
        public decimal BidAmount { get; set; }
        public DateTime Timestamp { get; set; }

        // Foreign keys
        public Guid ItemId { get; set; }
        public string UserId { get; set; } // Links to IdentityUser

        // Navigation properties
        public Item Item { get; set; }
        //public IdentityUser User { get; set; }
    }
}
