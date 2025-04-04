﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Utility;
namespace Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int Quantity {  get; set; }
        public ItemCondition Condition { get; set; } // Add this line
        public ICollection<Auction> Auctions { get; set; }=new List<Auction>();
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }  // Navigation property


    }


}
