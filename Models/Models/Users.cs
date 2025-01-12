using System.ComponentModel.DataAnnotations;

namespace BidBuzz.Models
{
    public class Users
    {
        [Key]
        public int ID {  get; set; }

        [Required]
        public String Name { get; set; }
        [Required]
        public String Email { get; set; }
        [Required]
        public String Role { get; set; }

    }
}
