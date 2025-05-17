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
            // 1) Pull only those Items where any of its Auctions is Approved or InAuction
            var items = await _unitOfWork.Items.GetAllAsync(
                filter: item => item.Auctions.Any(a =>
                    a.Status == AuctionStatus.Approved
                 || a.Status == AuctionStatus.InAuction),
                includeProperties: "Category,Auctions"
            );
            // Get the auction schedules for current and next week
            var currentWeekSchedule = await _unitOfWork.AuctionSchedules.GetFirstOrDefaultAsync(s => s.Week == "Current");
            var nextWeekSchedule = await _unitOfWork.AuctionSchedules.GetFirstOrDefaultAsync(s => s.Week == "Next");

            // Pass schedule data to the view using ViewBag
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

            // Get the auction status for this item
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == itemId);
            var auctionStatus = auction?.Status;

            var bids = await _unitOfWork.Bids.GetBidsForAuctionAsync(auction?.Id ?? 0);

            // Get the highest bid (first in the list)
            var highestBid = bids.FirstOrDefault();

            // Get the current user's auto bid if any
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isOwner = (userId != null && item.UserId == userId);

            AutoBid userAutoBid = null;

            if (auction != null && userId != null)
            {
                userAutoBid = await _unitOfWork.AutoBids.GetActiveAutoBidForUserAsync(auction.Id, userId);
            }

            // Create the ItemVM and return to the view
            var itemVM = new ItemVM
            {
                Item = item,
                ItemId = item.Id,
                AuctionStatus = auctionStatus,
                BidList = bids,  // All bids sorted by amount
                HighestAmount = highestBid?.Amount ?? 0,  // Highest bid amount
                HasActiveAutoBid = userAutoBid != null,
                MaxAutoBidAmount = userAutoBid?.MaxAmount,
                IsOwner = isOwner
            };

            // Include auto bid info in the BidModel
            itemVM.BidModel = new BidVM
            {
                ItemId = item.Id,
                Item = item,
                CurrentAutoBid = userAutoBid
            };

            return View(itemVM);
        }


        // GET: /Home/BidHistory
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
                    // Filter items based on selected status
                    if ((status == "all" && (auction.Status == AuctionStatus.Sold || auction.Status == AuctionStatus.InAuction)) ||
                        (status == "sold" && auction.Status == AuctionStatus.Sold) ||
                        (status == "inauction" && auction.Status == AuctionStatus.InAuction))
                    {
                        filteredItems.Add(item);
                    }
                }
            }

            // Pass the current status to the view through ViewBag
            ViewBag.CurrentStatus = status;

            // Count items in each category for the badge numbers
            ViewBag.AllCount = itemsWithAuctions.Count(i =>
                i.Auctions.Any(a => a.Status == AuctionStatus.Sold) &&
                i.Auctions.Any(a => a.Status == AuctionStatus.InAuction));
            ViewBag.SoldCount = itemsWithAuctions.Count(i => i.Auctions.Any(a => a.Status == AuctionStatus.Sold));
            ViewBag.InAuctionCount = itemsWithAuctions.Count(i => i.Auctions.Any(a => a.Status == AuctionStatus.InAuction));

            return View(filteredItems);
        }

        // GET: /Home/BidHistoryDetails?itemId=5
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

            return View(viewModel); // You will create this view
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

