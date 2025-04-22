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

        // GET: Admin Auction Management Page (Index)
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
                "Completed" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.Sold),
                "Cancelled" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.Cancelled),
                "Approved" => await _auctionRepo.GetAuctionsByStatusAsync(AuctionStatus.Approved),
                _ => (await _auctionRepo.GetAllAsync(includeProperties: "Item,Bids")).ToList()
            };

            return View(auctions);
        }

        // POST: Stop auction (Change status to Cancelled)
        [HttpPost]
        public async Task<IActionResult> StopAuction(int id)
        {
            await _auctionRepo.CancelAuctionAsync(id);
            return RedirectToAction(nameof(Index)); // Redirect to the Index action after stopping the auction
        }

        // GET: View Cancelled Auctions
        public async Task<IActionResult> Cancelled()
        {
            var cancelledAuctions = await _auctionRepo.GetCancelledAuctionsAsync();
            return View(cancelledAuctions); // This view is for showing cancelled auctions
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



        //POST: Update Next Week's Schedule
        [HttpPost]
        public async Task<IActionResult> EditSchedule(AuctionSchedule updatedSchedule)
        {
            if (ModelState.IsValid)
            {
                updatedSchedule.Week = "Next";
                await _scheduleRepo.UpdateScheduleAsync(updatedSchedule);
                TempData["Success"] = "Next week's auction schedule updated successfully.";
                return RedirectToAction(nameof(Index)); // Redirect to the main page after updating
            }
            return View(updatedSchedule); // If model state is invalid, return the view with errors
        }

        //GET: Current Auction Schedule View
        public async Task<IActionResult> CurrentSchedule()
        {
            var currentSchedule = await _scheduleRepo.GetScheduleAsync("Current");
            return View(currentSchedule); // View for showing the current auction schedule
        }

        // GET: Update the Auction Schedule for Next Week (form view)
        public async Task<IActionResult> UpdateScheduleForm()
        {
            
            var currentSchedule = await _scheduleRepo.GetScheduleAsync("Current");
            var nextSchedule = await _scheduleRepo.GetScheduleAsync("Next");

            if (nextSchedule == null)
            {
                
                return RedirectToAction(nameof(Index));
            }

            // Pass both schedules to the view to be displayed in the form
            ViewBag.CurrentSchedule = currentSchedule;
            ViewBag.NextSchedule = nextSchedule;

            return View(nextSchedule); // Display the form to edit next week's schedule
        }

        // POST: Submit Updated Schedule for Next Week
        [HttpPost]
        public async Task<IActionResult> UpdateSchedule(AuctionSchedule updatedSchedule)
        {
            if (ModelState.IsValid)
            {
                // Ensure the update is only for the next week's schedule
                updatedSchedule.Week = "Next";

                // Update the schedule in the repository
                await _scheduleRepo.UpdateScheduleAsync(updatedSchedule);

                // Notify the user of success
                TempData["Success"] = "Next week's auction schedule updated successfully.";

                // Redirect to the main auction management page after updating
                return RedirectToAction(nameof(Index));
            }

            // If the model state is invalid, re-display the form with errors
            return View(updatedSchedule);
        }

    }
}
