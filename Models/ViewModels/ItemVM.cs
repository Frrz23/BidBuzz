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
        public AuctionStatus? AuctionStatus { get; set; }
        public string? UserName { get; set; }
        public Auction? LatestAuction { get; set; }


    }
}
