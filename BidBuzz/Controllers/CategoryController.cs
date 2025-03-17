using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace BidBuzz.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(null);
            return View(categories);
        }


        public async Task<IActionResult> Upsert(int? id) {
            
            if (id == null || id == 0)
            {
                return View(new Category());
            }
            else
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Category category)
        {
            if (ModelState.IsValid) {
                if (category.Id == 0)
                {
                    await _unitOfWork.Categories.AddAsync(category);
                }
                else
                {
                    _unitOfWork.Categories.Update(category);

                }
                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            

        }
            return View(category);
        }

 
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
           
                _unitOfWork.Categories.Delete(id);
                await _unitOfWork.CompleteAsync();
            
            return RedirectToAction(nameof(Index));
        }
    }
  }
