using Microsoft.AspNetCore.Mvc;
using Models.Models;
using Models.Models.ViewModels;
using System.Threading.Tasks;
using DataAccess.Repository.IRepository;
using Utility;

namespace BidBuzz.Controllers
{
    public class AuctionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuctionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var auctions = await _unitOfWork.Auctions.GetAllAsync("Item");
            if (!auctions.Any())
            {
                Console.WriteLine("No auctions found in the database.");
            }
            foreach (var auction in auctions)
            {
                Console.WriteLine($"Auction ID: {auction.Id}, Item: {auction.Item?.Name ?? "No Item"}");
            }
            return View(auctions);
        }

        public async Task<IActionResult> Update(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id, "Item");

            if (auction == null)
            {
                return NotFound();
            }

            var auctionVM = new AuctionVM
            {
                Auction = auction,
                Item = auction.Item
            };

            return View(auctionVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AuctionVM auctionVM)
        {
            if (ModelState.IsValid)
            {
                var existingAuction = await _unitOfWork.Auctions.GetByIdAsync(auctionVM.Auction.Id);
                if (existingAuction == null)
                {
                    return NotFound();
                }

                existingAuction.Status = auctionVM.Auction.Status;
                existingAuction.StartTime = auctionVM.Auction.StartTime;
                existingAuction.EndTime = auctionVM.Auction.EndTime;

                _unitOfWork.Auctions.Update(existingAuction);
                await _unitOfWork.CompleteAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(auctionVM);
        }

        [HttpPost]
        public async Task<IActionResult> StopAuction(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null)
            {
                return NotFound();
            }

            auction.Status = AuctionStatus.Sold;
            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
