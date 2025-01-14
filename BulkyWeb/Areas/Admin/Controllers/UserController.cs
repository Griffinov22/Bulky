using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManger;
        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManger = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string? userId)
        {
            ApplicationUser? user = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            string? roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            IEnumerable<SelectListItem> companyList = _db.Companies.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            IEnumerable<SelectListItem> roleList = _db.Roles.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

            RoleManagementVM rmvm = new()
            {
                User = user,
                CompanyList = companyList,
                RoleList = roleList,
                RoleId = roleId
            };

            return View(rmvm);
        }
        [HttpPost]
        public async Task<IActionResult> RoleManagement(RoleManagementVM rmvm)
        {
            if (rmvm.RoleId is null) return NotFound();

            ApplicationUser? user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == rmvm.User.Id);
            if (user is null) return NotFound();

            string currRoleId = _db.UserRoles.First(u => u.UserId == user.Id).RoleId;
            string oldRoleName = _db.Roles.First(r => r.Id == currRoleId).Name;
            string newRoleName = _db.Roles.First(r => r.Id == rmvm.RoleId).Name;

            if (newRoleName == SD.Role_Company && rmvm.CompanyId is null)
            {
                ModelState.AddModelError("CompanyId", "Company users must have a selected company");

                // Re-populate the CompanyList and RoleList before returning the view
                rmvm.CompanyList = _db.Companies.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
                rmvm.RoleList = _db.Roles.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

                return View(rmvm);
            }
            else if (newRoleName == SD.Role_Company && rmvm.CompanyId != null)
            {
                Company? c = _db.Companies.FirstOrDefault(c => c.Id == rmvm.CompanyId);

                if (c is null) return NotFound(); // prevent overriding current user and roles

                user.CompanyId = c.Id;
            }
            else
            {
                user.CompanyId = null;

            }

            _db.ApplicationUsers.Update(user);
            await _db.SaveChangesAsync();

            await _userManger.RemoveFromRoleAsync(user, oldRoleName);
            await _userManger.AddToRoleAsync(user, newRoleName);

            TempData["success"] = "successfully updated user role";
            return RedirectToAction("Index");
        }

        #region APICALLS

        [HttpGet]
        public JsonResult GetAll()
        {
            ClaimsIdentity user = (ClaimsIdentity)User.Identity;
            string userId = user.FindFirst(ClaimTypes.NameIdentifier).Value;

            List<ApplicationUser> users = _db.ApplicationUsers.Include(u => u.Company).OrderBy((user) => user.Id == userId).ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (ApplicationUser userObj in users)
            {
                string roleId = userRoles.First(r => r.UserId == userObj.Id).RoleId;
                string userRole = roles.First(r => r.Id == roleId).Name!;
                userObj.Role = userRole;

                if (userObj.Company is null)
                {
                    userObj.Company = new()
                    {
                        Name = "N/A"
                    };
                }
            }


            return Json(new { data = users });
        }

        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody] string? id)
        {

            ApplicationUser? user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);

            if (user == null) return Json(new { success = false, message = "Error while Locking/unlocking user" });

            DateTimeOffset? lockState = user.LockoutEnd;
            Boolean locked = false;

            if (lockState.HasValue && lockState > DateTime.UtcNow)
            {
                // user is locked and needs to be unlocked
                user.LockoutEnd = DateTime.UtcNow;
                locked = true;
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(1);
            }

            _db.ApplicationUsers.Update(user);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"User {(locked ? "unlocked" : "locked")} successfully" });

        }

        #endregion
    }
}
