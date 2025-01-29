using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositary
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; }
        IItemRepository Item { get; }
        Task<int> SaveAsync();
    }

}
