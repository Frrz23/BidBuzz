using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModels
{
    public class BidVM
    {
        public Bid Bid { get; set; }  // Full Bid model
        public string UserName { get; set; }  // Extra property (user's name)
    }

}
