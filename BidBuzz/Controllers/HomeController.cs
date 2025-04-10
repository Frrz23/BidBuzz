using System.Diagnostics;
using Models;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;
using Utility;

namespace BidBuzz.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Item> item = await _unitOfWork.Items.GetAllAsync(includeProperties: "Category");
            return View(item);
        }

            public async Task<IActionResult> Details(int itemId)
            {
                var item = await _unitOfWork.Items.GetByIdAsync(itemId);
                if (item == null)
                {
                    return NotFound();
                }

                // Get the auction status for this item
                var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
                var auctionStatus = auction?.Status;

                // Create the ItemVM and return to the view
                var itemVM = new ItemVM
                {
                    Item = item,
                    AuctionStatus = auctionStatus
                };

                return View(itemVM);
            }
        






        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
