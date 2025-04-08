using System.Diagnostics;
using Models;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;

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
            // Fetch the item
            var item = await _unitOfWork.Items.GetFirstOrDefaultAsync(i => i.Id == itemId, includeProperties: "Category");
            if (item == null)
            {
                return NotFound();
            }

            
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);

            var viewModel = new ItemVM
            {
                Item = item,
                LatestAuction = auction  
            };



            return View(viewModel);
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
