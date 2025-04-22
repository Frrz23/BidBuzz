using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Utility;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace Models
{
    public class Item
    {
        [Key] // Primary key
        public int Id { get; set; }

        [Required]
        [MaxLength(200)] // Name must not exceed 200 characters
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)] // Description must not exceed 1000 characters
        public string Description { get; set; } = string.Empty;


        
        [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0.")]
        public decimal StartingPrice { get; set; }

        [ValidateNever]
        public string? ImageUrl { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Please select an item condition.")]
        public ItemCondition? Condition { get; set; } 

        public ICollection<Auction> Auctions { get; set; } = new List<Auction>();

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; } // Navigation property




    }


}
