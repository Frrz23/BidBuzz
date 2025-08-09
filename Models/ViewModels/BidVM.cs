using System;
using System.ComponentModel.DataAnnotations;
using Models;

namespace Models.ViewModels
{
    public class BidVM
    {
        public Bid Bid { get; set; }  
        public string UserName { get; set; }  

        [Required(ErrorMessage = "Please enter your bid amount.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid must be greater than zero.")]
        public decimal BidAmount { get; set; }  

        public int ItemId { get; set; }
        public Item Item { get; set; }

        
        [Display(Name = "Enable Auto Bidding")]
        public bool EnableAutoBid { get; set; }

        [Display(Name = "Maximum Bid Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Maximum bid must be greater than zero.")]
        public decimal MaxBidAmount { get; set; }

        
        public AutoBid CurrentAutoBid { get; set; }
    }
}