using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models.ViewModels
{
    public class AuctionVM
    {
        public Auction Auction { get; set; }  // Full Auction model
        public Item Item { get; set; }  // Item associated with the auction

    }
}
