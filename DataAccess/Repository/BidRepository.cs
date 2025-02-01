using DataAccess.Data;
using DataAccess.Repositary;
using DataAccess.Repository.IRepository;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    public class BidRepository : Repository<Bid>,IBidRepository
    {
        private readonly ApplicationDbContext _context;

        public BidRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        //public async Task GetHighestBid()
        //{
           
        //}   


    }
}
