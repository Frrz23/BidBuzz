using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

}
