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
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } // PendingApproval, Approved, InAuction
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ImageUrl { get; set; }

        // Foreign Keys
        //public int CategoryID { get; set; }
        //public int? UserID { get; set; } // Nullable for Admin-added items

        //// Navigation Properties
        //public Category Category { get; set; }
    }

}
