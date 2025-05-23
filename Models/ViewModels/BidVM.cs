﻿using System;
using System.ComponentModel.DataAnnotations;
using Models;

namespace Models.ViewModels
{
    public class BidVM
    {
        public Bid Bid { get; set; }  // Full Bid model
        public string UserName { get; set; }  // Extra property (user's name)

        [Required(ErrorMessage = "Please enter your bid amount.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid must be greater than zero.")]
        public decimal BidAmount { get; set; }  // used only for input binding and validation

        public int ItemId { get; set; }
        public Item Item { get; set; }

        // Auto bid properties
        [Display(Name = "Enable Auto Bidding")]
        public bool EnableAutoBid { get; set; }

        [Display(Name = "Maximum Bid Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Maximum bid must be greater than zero.")]
        public decimal MaxBidAmount { get; set; }

        // Will be set if there's an active auto bid for this user
        public AutoBid CurrentAutoBid { get; set; }
    }
}