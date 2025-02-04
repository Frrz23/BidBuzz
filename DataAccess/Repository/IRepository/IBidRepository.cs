﻿using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository.IRepository
{
    public interface IBidRepository : IRepository<Bid>
    {
        Task <IEnumerable<Bid>> GetBidsByItemIdAsync(int itemId);
        Task<Bid> GetHighestBidAsync(int itemId);
    }

}


//Task==asyncronous Operation that may complete in future 
