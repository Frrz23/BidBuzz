using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModels
{
    public class PaymentVM
    {
        public List<Auction> ToPay { get; set; } = new List<Auction>();
        public List<Auction> Paid { get; set; } = new List<Auction>();
    }
}