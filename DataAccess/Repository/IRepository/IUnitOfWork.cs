using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositary
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Category> Categories { get; }
        IRepository<Item> Items { get; }
        IRepository<Bid> Bids { get; }
        IRepository<Auction> Auctions { get; }

        Task<int> CompleteAsync(); // Saves changes to the database
    }
}