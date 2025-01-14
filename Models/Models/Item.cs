using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal StartingPrice { get; set; }
        public DateTime AuctionEndTime { get; set; }

        // Foreign keys
        public string SellerId { get; set; } // Links to IdentityUser
        public Guid CategoryId { get; set; }

        // Navigation properties
        //public IdentityUser Seller { get; set; }
        public Category Category { get; set; }
        public ICollection<Bid> Bids { get; set; }
    }
}
