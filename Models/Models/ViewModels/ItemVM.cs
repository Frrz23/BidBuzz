using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace Models.Models.ViewModels
{
    public class ItemVM
    {
        public Item Item { get; set; }  // Full Item model
        public string CategoryName { get; set; }
        public AuctionStatus? AuctionStatus { get; set; }
    }
}
