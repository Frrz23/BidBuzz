using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace DataAccess.Repositary
{
    public interface IItemRepository : IRepository<Item>
    {
        Task <IEnumerable<Item>> GetItemsByStatusAsync(ItemStatus status);

    }

}
