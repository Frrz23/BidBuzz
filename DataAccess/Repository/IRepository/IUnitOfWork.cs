using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; }
        IItemRepository Items { get; }
        IBidRepository Bids { get; }
        IAuctionRepository Auctions { get; }

        Task<int> CompleteAsync(); // Saves changes to the database
    }
}