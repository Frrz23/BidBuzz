using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? RelatedItemId { get; set; }
        public string? RelatedItemName { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
