﻿using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Models.Models;
using Models.Models.ViewModels;
using Utility;

namespace BidBuzz.Controllers
{
    public class ItemController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AuctionScheduleConfig _auctionSchedule;


        public ItemController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IOptions<AuctionScheduleConfig> auctionScheduleConfig)//ioptions is used to get data from appsetiings without it we cant get it 
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _auctionSchedule = auctionScheduleConfig.Value; // Extract value from IOptions
        }


        public async Task<IActionResult> Index()
        {
            var items = await _unitOfWork.Items.GetAllAsync(includeProperties: "Category,Auctions");

            var itemVMs = items.Select(i => new ItemVM
            {
                Item = i,
                CategoryName = i.Category.Name,
                AuctionStatus = i.Auctions.OrderByDescending(a => a.StartTime).FirstOrDefault()?.Status
            }).ToList();

            return View(itemVMs);
        }



        //public IActionResult Create()
        //{
        //    return View();
        //}

        //// POST: Item/Create
        ////[HttpPost]
        ////[ValidateAntiForgeryToken]
        ////public async Task<IActionResult> Create(Item item, IFormFile? file)
        ////{
        ////    if (ModelState.IsValid)
        ////    {
        ////        string wwwRootPath = _webHostEnvironment.WebRootPath;
        ////        if (file != null)
        ////        {
        ////            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        ////            string productPath = Path.Combine(wwwRootPath, @"images");

        ////            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
        ////            {
        ////                file.CopyToAsync(fileStream);
        ////            }
        ////            item.ImageUrl = @"\images\" + fileName;
        ////        }
        ////        else
        ////        {
        ////            item.ImageUrl = null; // ✅ Allow null images
        ////        }
        ////        item.Status = AuctionStatus.PendingApproval;
        ////        await _unitOfWork.Items.AddAsync(item);
        ////        await _unitOfWork.CompleteAsync();
        ////        return RedirectToAction(nameof(Index));

        ////    }
        ////    return View(item);
        ////}

        ////// GET: Item/Edit/5
        ////public async Task<IActionResult> Edit(int id)
        ////{

        ////    var item = await _unitOfWork.Items.GetByIdAsync(id);
        ////    if (item == null)
        ////    {
        ////        return NotFound();
        ////    }
        ////    return View(item);
        ////}

        //// POST: Item/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, Item item, IFormFile? file)
        //{
        //    if (item.Status != AuctionStatus.PendingApproval)
        //    {
        //        return BadRequest("You can't edit items once they are approved or in auction.");
        //    }

        //    if (id != item.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        string wwwRootPath = _webHostEnvironment.WebRootPath;

        //        if (file != null)
        //        {
        //            // Delete old image if exists
        //            if (!string.IsNullOrEmpty(item.ImageUrl))
        //            {
        //                var oldImagePath = Path.Combine(wwwRootPath, item.ImageUrl.TrimStart('\\'));
        //                if (System.IO.File.Exists(oldImagePath))
        //                {
        //                    System.IO.File.Delete(oldImagePath);
        //                }
        //            }

        //            // Save new image
        //            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        //            string filePath = Path.Combine(wwwRootPath, @"images", fileName);

        //            using (var fileStream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await file.CopyToAsync(fileStream);
        //            }

        //            item.ImageUrl = @"\images\" + fileName; // Store relative path
        //        }

        //        _unitOfWork.Items.Update(item);
        //        await _unitOfWork.CompleteAsync();
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(item);
        //}

        public async Task<IActionResult> Upsert(int? id)
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                ModelState.AddModelError("", "No categories available. Please add a category first.");
                return View(new Item()); // Prevents submitting without valid categories
            }
            ViewBag.Categories = categories;
            Item item = new Item();

            if (id == null || id == 0)
            {
                // Create mode
                return View(item);
            }
            else
            {
                // Edit mode
                item = await _unitOfWork.Items.GetByIdAsync(id.Value);
                if (item == null)
                {
                    return NotFound();
                }
                return View(item);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ItemVM item, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                var startTime = AuctionScheduleHelper.GetNextAuctionStart(_auctionSchedule.StartDay, _auctionSchedule.StartHour);
                var endTime = AuctionScheduleHelper.GetNextAuctionEnd(_auctionSchedule.EndDay, _auctionSchedule.EndHour);

                string wwwRootPath = _webHostEnvironment.WebRootPath;
               

                if (file != null)
                {
                    string productPath = Path.Combine(wwwRootPath, @"images");

                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    // Delete old image if editing
                    if (!string.IsNullOrEmpty(item.Item.ImageUrl) && item.Item.Id != 0)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, item.Item.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    item.Item.ImageUrl = @"\images\" + fileName;
                }

                if (item.Item.Id == 0)
                {
                    // Creating new item
                    
                    await _unitOfWork.Items.AddAsync(item.Item);
                    await _unitOfWork.CompleteAsync();

                    // Create auction for the item
                    var auction = new Auction
                    {
                        ItemId = item.Item.Id,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = AuctionStatus.PendingApproval
                    };
                    await _unitOfWork.Auctions.AddAsync(auction);
                }
                else
                {
                    // Editing existing item
                    var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == item.Item.Id);
                    if (auction != null)
                    {
                        
                        _unitOfWork.Auctions.Update(auction);
                    }
                    _unitOfWork.Items.Update(item.Item);
                }

                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }

            var categories = await _unitOfWork.Categories.GetAllAsync();
            ViewBag.Categories = categories;
            return View(item);
        }

        // GET: Item/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(id);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == item.Id);

            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(id);
            var auction = await _unitOfWork.Auctions.GetFirstOrDefaultAsync(a => a.ItemId == item.Id);
            if (item != null)
            {
                // Delete the associated image file if it exists
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                // Now delete the item from the database
                _unitOfWork.Items.Delete(item.Id);
                if (auction != null)
                {
                    _unitOfWork.Auctions.Delete(auction.Id);
                }

                await _unitOfWork.CompleteAsync();
            }
           
            

            return RedirectToAction(nameof(Index));
        }

    }
}
