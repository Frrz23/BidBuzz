using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models.Models;
using Utility;

namespace BidBuzz.Controllers
{
    public class ItemController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public ItemController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var items = await _unitOfWork.Items.GetAllAsync(includeProperties: "Category");
            return View(items);
        }

        //public IActionResult Create()
        //{
        //    return View();
        //}

        //// POST: Item/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Item item, IFormFile? file)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        string wwwRootPath = _webHostEnvironment.WebRootPath;
        //        if (file != null)
        //        {
        //            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        //            string productPath = Path.Combine(wwwRootPath, @"images");

        //            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
        //            {
        //                file.CopyToAsync(fileStream);
        //            }
        //            item.ImageUrl = @"\images\" + fileName;
        //        }
        //        else
        //        {
        //            item.ImageUrl = null; // ✅ Allow null images
        //        }
        //        item.Status = AuctionStatus.PendingApproval;
        //        await _unitOfWork.Items.AddAsync(item);
        //        await _unitOfWork.CompleteAsync();
        //        return RedirectToAction(nameof(Index));

        //    }
        //    return View(item);
        //}

        //// GET: Item/Edit/5
        //public async Task<IActionResult> Edit(int id)
        //{

        //    var item = await _unitOfWork.Items.GetByIdAsync(id);
        //    if (item == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(item);
        //}

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
        public async Task<IActionResult> Upsert(Item item, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
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
                    if (!string.IsNullOrEmpty(item.ImageUrl) && item.Id != 0)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, item.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    item.ImageUrl = @"\images\" + fileName;
                }

                if (item.Id == 0)
                {
                    // New item creation
                    item.Status = AuctionStatus.PendingApproval;
                    await _unitOfWork.Items.AddAsync(item);
                }
                else
                {
                    // Editing existing item
                    _unitOfWork.Items.Update(item);
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
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }
        public async Task<IActionResult> Approve(int id)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            item.Status = AuctionStatus.Approved;
            _unitOfWork.Items.Update(item);
            await _unitOfWork.CompleteAsync();
            return RedirectToAction(nameof(Index));
        }

  


        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _unitOfWork.Items.GetByIdAsync(id);
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
                await _unitOfWork.CompleteAsync();
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
