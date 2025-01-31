using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository.IRepository
{
    public interface IBidRepository<T> where T : class
    {
        Task GetHighestBid();
    }

}


//Task==asyncronous Operation that may complete in future 
