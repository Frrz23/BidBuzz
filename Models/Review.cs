using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        [Required]
        public string ReviewerId { get; set; }

        [ForeignKey(nameof(ReviewerId))]
        public ApplicationUser Reviewer { get; set; }

        [Required]
        public string ReviewedUserId { get; set; }

        [ForeignKey(nameof(ReviewedUserId))]
        public ApplicationUser ReviewedUser { get; set; }

        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
