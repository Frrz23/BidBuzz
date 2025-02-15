using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models.Models;
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
            var auction = await _unitOfWork.Auctions.GetAllAsync();
            return View(auction);
        }


        // Upsert (Create/Edit)
        public async Task<IActionResult> Upsert(int? id)
        {
            Auction auction = id == null || id == 0 ? new Auction() : await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null)
            {
                return NotFound();
            }
            ViewBag.Items = (await _unitOfWork.Items.GetAllAsync())
                    .Select(i => new { i.Id, i.Name })
                    .ToList();

            return View(auction);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Auction auction)
        {
            if (ModelState.IsValid)
            {
                if (auction.Id == 0)
                {
                    await _unitOfWork.Auctions.AddAsync(auction);
                }
                else
                {
                    _unitOfWork.Auctions.Update(auction);
                }
                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(auction);
        }

        // Start Auction
        public async Task<IActionResult> StartAuction(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null || auction.Status != AuctionStatus.Approved)
            {
                return NotFound();
            }

            auction.Status = AuctionStatus.InAuction;
            auction.StartTime = DateTime.Now;
            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();

            return RedirectToAction(nameof(Index));
        }

        // End Auction
        public async Task<IActionResult> EndAuction(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null || auction.Status != AuctionStatus.InAuction)
            {
                return NotFound();
            }

            auction.Status = AuctionStatus.Sold;
            auction.EndTime = DateTime.Now;
            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Delete(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null)
            {
                return NotFound();
            }
            return View(auction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
           
                _unitOfWork.Auctions.Delete(id);
                await _unitOfWork.CompleteAsync();
            
            return RedirectToAction(nameof(Index));
        }
    }
  }
