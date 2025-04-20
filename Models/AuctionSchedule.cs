using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class AuctionSchedule
    {
        [Key]
        public int Id { get; set; }

        // "Current" or "Next" to differentiate between active and upcoming schedule
        [Required]
        public string Week { get; set; } = "Next";

        [Required]
        public string StartDay { get; set; } = "Saturday"; // e.g., "Saturday"

        [Required]
        public int StartHour { get; set; } = 12; // 24-hour format

        [Required]
        public string EndDay { get; set; } = "Sunday";

        [Required]
        public int EndHour { get; set; } = 1;
    }

}
