using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        [Required(ErrorMessage = "Please enter your bid amount.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid must be greater than zero.")]
        public decimal BidAmount { get; set; }  // used only for input binding and validation
        public List<Bid> BidList { get; set; } = new(); // Include user navigation






    }
}
