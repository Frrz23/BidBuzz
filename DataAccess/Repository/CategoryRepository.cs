using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task <IEnumerable<Item>>GetItemsByCategoryAsync(int categoryId)
        {
            return await _context.Items.Where(i=>i.CategoryId==categoryId).ToListAsync();

        }
    }

}
