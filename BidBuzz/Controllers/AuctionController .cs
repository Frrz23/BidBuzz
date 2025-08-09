using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Utility;

namespace BidBuzz.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuctionController : Controller
    {
        private readonly IAuctionRepository _auctionRepo;
        private readonly IAuctionScheduleRepository _scheduleRepo;

        public AuctionController(IAuctionRepository auctionRepo, IAuctionScheduleRepository scheduleRepo)
        {
            _auctionRepo = auctionRepo;
            _scheduleRepo = scheduleRepo;
        }

        public async Task<IActionResult> Index(string status = "All")
        {
            ViewBag.SelectedStatus = status;

            var currentSchedule = await _scheduleRepo.GetScheduleAsync("Current");
            var nextSchedule = await _scheduleRepo.GetScheduleAsync("Next");

            ViewBag.CurrentSchedule = currentSchedule;
            ViewBag.NextSchedule = nextSchedule;

            List<Auction> auctions = status switch
            {
                "InAuction" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.InAuction),
                "Sold" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.Sold),
                "Cancelled" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.Cancelled),
                "Approved" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.Approved),
                _ => (await _auctionRepo.GetAllAsync(includeProperties: "Item,Bids")).ToList()
            };

            return View(auctions);
        }

        [HttpPost]
        public async Task<IActionResult> StopAuction(int id)
        {
            await _auctionRepo.CancelAuctionAsync(id);
            return RedirectToAction(nameof(Index)); 
        }

        public async Task<IActionResult> Cancelled()
        {
            var cancelledAuctions = await _auctionRepo.GetCancelledAuctionsAsync();
            return View(cancelledAuctions); 
        }

        
        public async Task<IActionResult> EditSchedule()
        {
            Console.WriteLine("🔥 EditSchedule GET triggered");

            var nextSchedule = await _scheduleRepo.GetScheduleAsync("Next");
            if (nextSchedule == null)
            {
                TempData["Error"] = "No schedule found for next week.";
                return RedirectToAction(nameof(Index));
            }

            return View(nextSchedule);
        }



        [HttpPost]
        public async Task<IActionResult> EditSchedule(AuctionSchedule updatedSchedule)
        {
            if (ModelState.IsValid)
            {
                updatedSchedule.Week = "Next";
                await _scheduleRepo.UpdateScheduleAsync(updatedSchedule);
                TempData["Success"] = "Next week's auction schedule updated successfully.";
                return RedirectToAction(nameof(Index)); 
            }
            return View(updatedSchedule); 
        }


        public async Task<IActionResult> CurrentSchedule()
        {
            var currentSchedule = await _scheduleRepo.GetScheduleAsync("Current");
            return View(currentSchedule); 
        }


        [HttpPost]
        public async Task<IActionResult> UpdateSchedule(AuctionSchedule updatedSchedule)
        {
            if (ModelState.IsValid)
            {
                updatedSchedule.Week = "Next";

                await _scheduleRepo.UpdateScheduleAsync(updatedSchedule);

                TempData["Success"] = "Next week's auction schedule updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(updatedSchedule);
        }

    }
}
