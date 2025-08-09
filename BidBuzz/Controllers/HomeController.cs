using System.Diagnostics;
using Models;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;
using Utility;
using Microsoft.AspNetCore.Authorization;
using Stripe.Checkout;
using Microsoft.Extensions.Options;
using Azure;
using System.Security.Claims;

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
            var items = await _unitOfWork.Items.GetAllAsync(
                filter: item => item.Auctions.Any(a =>
                    a.Status == AuctionStatus.Approved
                 || a.Status == AuctionStatus.InAuction),
                includeProperties: "Category,Auctions"
            );
            var currentWeekSchedule = await _unitOfWork.AuctionSchedules.GetFirstOrDefaultAsync(s => s.Week == "Current");
            var nextWeekSchedule = await _unitOfWork.AuctionSchedules.GetFirstOrDefaultAsync(s => s.Week == "Next");

            ViewBag.CurrentWeekSchedule = currentWeekSchedule;
            ViewBag.NextWeekSchedule = nextWeekSchedule;

            return View(items);
        }


        [Authorize]
        public async Task<IActionResult> Details(int itemId)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(itemId, includeProperties: "Category");
            if (item == null)
            {
                return NotFound();
            }

            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            var auctionStatus = auction?.Status;

            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction?.Id ?? 0);

            var highestBid = bids.FirstOrDefault();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isOwner = (userId != null && item.UserId == userId);

            AutoBid userAutoBid = null;

            if (auction != null && userId != null)
            {
                userAutoBid = await _unitOfWork.AutoBids.GetActiveAutoBidForUserAsync(auction.Id, userId);
            }

            var itemVM = new ItemVM
            {
                Item = item,
                ItemId = item.Id,
                AuctionStatus = auctionStatus,
                BidList = bids,  
                HighestAmount = highestBid?.Amount ?? 0,
                HasActiveAutoBid = userAutoBid != null,
                MaxAutoBidAmount = userAutoBid?.MaxAmount,
                IsOwner = isOwner
            };

            itemVM.BidModel = new BidVM
            {
                ItemId = item.Id,
                Item = item,
                CurrentAutoBid = userAutoBid
            };

            return View(itemVM);
        }


        [Authorize]
        public async Task<IActionResult> BidHistory(string status = "all")
        {
            var itemsWithAuctions = await _unitOfWork.Items.GetAllAsync(includeProperties: "Category,Auctions");
            var filteredItems = new List<Item>();

            foreach (var item in itemsWithAuctions)
            {
                var auction = item.Auctions.FirstOrDefault();
                if (auction != null)
                {
                    if ((status == "all" && (auction.Status == AuctionStatus.Sold || auction.Status == AuctionStatus.InAuction)) ||
                        (status == "sold" && auction.Status == AuctionStatus.Sold) ||
                        (status == "inauction" && auction.Status == AuctionStatus.InAuction))
                    {
                        filteredItems.Add(item);
                    }
                }
            }

            ViewBag.CurrentStatus = status;

            ViewBag.AllCount = itemsWithAuctions.Count(i =>
                i.Auctions.Any(a => a.Status == AuctionStatus.Sold) &&
                i.Auctions.Any(a => a.Status == AuctionStatus.InAuction));
            ViewBag.SoldCount = itemsWithAuctions.Count(i => i.Auctions.Any(a => a.Status == AuctionStatus.Sold));
            ViewBag.InAuctionCount = itemsWithAuctions.Count(i => i.Auctions.Any(a => a.Status == AuctionStatus.InAuction));

            return View(filteredItems);
        }

        public async Task<IActionResult> BidHistoryDetails(int itemId)
        {
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            if (auction == null)
            {
                TempData["Error"] = "No auction found for the selected item.";
                return RedirectToAction("BidHistory");
            }

            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction.Id);

            var item = await _unitOfWork.Items.GetByIdAsync(itemId, includeProperties: "Category");

            var viewModel = new ItemVM
            {
                Item = item,
                BidList = bids,
                AuctionStatus = auction.Status
            };

            return View(viewModel); 
        }





        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult Contact()
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

