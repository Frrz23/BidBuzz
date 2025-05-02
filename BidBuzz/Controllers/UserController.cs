using DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models.ViewModels;
using Models;
using Utility;


namespace Quillia.Areas.Admin.Controllers
{

    
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            var vm = new UserProfileVm
            {
                Id = user.Id,
                Full_Name = user.Full_Name,
                Age = user.Age,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(UserProfileVm vm)
        {
            // We'll handle basic profile validation normally
            bool profileValid = true;

            // Check for duplicate Full_Name
            var duplicateName = await _db.ApplicationUsers
                .AnyAsync(u => u.Full_Name == vm.Full_Name && u.Id != vm.Id);

            if (duplicateName)
            {
                ModelState.AddModelError("Full_Name", "This name is already taken by another user.");
                profileValid = false;
            }

            // Validate all non-password fields
            if (string.IsNullOrWhiteSpace(vm.Full_Name) ||
                vm.Age < 0 || vm.Age > 120 ||
                string.IsNullOrWhiteSpace(vm.PhoneNumber) ||
                !System.Text.RegularExpressions.Regex.IsMatch(vm.PhoneNumber, @"^\d{10}$") ||
                string.IsNullOrWhiteSpace(vm.Address))
            {
                profileValid = false;
            }

            if (!profileValid)
            {
                return View(vm);
            }

            // At this point, profile data is valid
            var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == vm.Id);
            if (user == null) return NotFound();

            // Update basic profile fields
            user.Full_Name = vm.Full_Name;
            user.Age = vm.Age;
            user.PhoneNumber = vm.PhoneNumber;
            user.Address = vm.Address;

            await _db.SaveChangesAsync();

            // Handle password separately - all fields must be filled or all must be empty
            bool passwordChangeRequested = !string.IsNullOrWhiteSpace(vm.CurrentPassword) ||
                                         !string.IsNullOrWhiteSpace(vm.NewPassword) ||
                                         !string.IsNullOrWhiteSpace(vm.ConfirmPassword);

            // If password change was requested and client-side validation passed, process it
            if (passwordChangeRequested)
            {
                // Double-check that all required fields are provided
                if (string.IsNullOrWhiteSpace(vm.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(vm.NewPassword) ||
                    string.IsNullOrWhiteSpace(vm.ConfirmPassword))
                {
                    // Client-side validation should prevent this, but just in case
                    TempData["error"] = "All password fields are required to change password.";
                    return View(vm);
                }

                // Check that passwords match (again, client-side validation should catch this)
                if (vm.NewPassword != vm.ConfirmPassword)
                {
                    TempData["error"] = "New password and confirmation do not match.";
                    return View(vm);
                }

                // Attempt to change password
                var identityUser = await _userManager.FindByIdAsync(vm.Id);
                var pwdResult = await _userManager.ChangePasswordAsync(
                    identityUser,
                    vm.CurrentPassword,
                    vm.NewPassword
                );

                if (!pwdResult.Succeeded)
                {
                    // Show specific error message from Identity
                    TempData["error"] = "Password change failed: " +
                        string.Join(", ", pwdResult.Errors.Select(e => e.Description));
                    return View(vm);
                }

                TempData["success"] = "Profile and password updated successfully!";
            }
            else
            {
                TempData["success"] = "Profile updated successfully!";
            }

            return RedirectToAction("Index", "Home");
        }



        public IActionResult RoleManagment(string userId)
        {
            string roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userId)?.RoleId;

            RoleManagmentVM roleVM = new RoleManagmentVM()
            {
                ApplicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId),
                RoleList = _db.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                })
            };

            roleVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == roleId)?.Name;

            return View(roleVM);
        }

        [HttpPost]
        public async Task<IActionResult> RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            string roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == roleManagmentVM.ApplicationUser.Id)?.RoleId;
            string oldRole = _db.Roles.FirstOrDefault(u => u.Id == roleId)?.Name;

            if (roleManagmentVM.ApplicationUser.Role != oldRole)
            {
                var applicationUser = await _userManager.FindByIdAsync(roleManagmentVM.ApplicationUser.Id);

                if (!string.IsNullOrEmpty(oldRole))
                {
                    await _userManager.RemoveFromRoleAsync(applicationUser, oldRole);
                }

                await _userManager.AddToRoleAsync(applicationUser, roleManagmentVM.ApplicationUser.Role);
            }

            return RedirectToAction("Index");
        }


        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _db.ApplicationUsers.ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in objUserList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id)?.RoleId;
                user.Role = roles.FirstOrDefault(u => u.Id == roleId)?.Name;
            }

            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // Unlock user
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                // Lock user
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }

            _db.SaveChanges();
            return Json(new { success = true, message = "Operation Successful" });
        }

        #endregion
    }
}