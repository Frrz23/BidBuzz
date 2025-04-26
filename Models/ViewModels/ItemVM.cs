using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace Models.ViewModels
{
    public class ItemVM
    {
        public Item Item { get; set; }  // Full Item model
        public int ItemId { get; set; } // <-- Add this

        public AuctionStatus? AuctionStatus { get; set; }
        public string? UserName { get; set; }

        public List<Bid> BidList { get; set; } = new(); // Include user navigation

        public decimal HighestAmount { get; set; }  // used only for input binding and validation

        [ValidateNever]
        public BidVM? BidModel { get; set; } = new();
        public int RemainingRelistAttempts { get; set; }





    }
}
