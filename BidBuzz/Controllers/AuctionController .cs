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


        public async Task<IActionResult> Details(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null)
                return NotFound();

            return View(auction);
        }

        // Approve an auction
        public async Task<IActionResult> Approve(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null)
                return NotFound();

            auction.Status = AuctionStatus.Approved;
            _unitOfWork.Auctions.Update(auction);
            await _unitOfWork.CompleteAsync();

            return RedirectToAction(nameof(Index));
        }

        // Start an auction
        public async Task<IActionResult> Start(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null || auction.Status != AuctionStatus.Approved)
                return NotFound();

            await _unitOfWork.Auctions.StartAuctionAsync(id);
            await _unitOfWork.CompleteAsync();

            return RedirectToAction(nameof(Index));
        }

        // End an auction
        public async Task<IActionResult> End(int id)
        {
            var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
            if (auction == null || auction.Status != AuctionStatus.InAuction)
                return NotFound();

            await _unitOfWork.Auctions.EndAuctionAsync(id);
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
