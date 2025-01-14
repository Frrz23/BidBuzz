﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        // Navigation property
        public ICollection<Item> Items { get; set; }

    }
}
