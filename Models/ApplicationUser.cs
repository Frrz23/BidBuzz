using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ApplicationUser:IdentityUser
    {
        public string Full_Name { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        [NotMapped]
        public string Role { get; set; }

    }
}
