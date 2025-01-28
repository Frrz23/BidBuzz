using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
  
        public class Bid
        {
            public int BidID { get; set; }
            public decimal BidAmount { get; set; }
            public DateTime BidTime { get; set; }

            // Foreign Keys
            //public int ItemID { get; set; }
            //public int UserID { get; set; }

            // Navigation Properties
            //public Item Item { get; set; }
        }

    }
