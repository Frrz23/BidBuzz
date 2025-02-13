using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task <IEnumerable<Item>>GetItemsByCategoryAsync(int categoryId);
    }

}
