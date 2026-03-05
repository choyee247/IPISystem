using System.Security.Claims;
//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Services.Interface;
using ProjectManagementSystem.ViewModels;

namespace ProjectManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly PMSDbContext _context;

        public AdminController(PMSDbContext context)
        {
            _context = context;
        }
        //public IActionResult Dashboard()
        //{
        //    // Check role in session
        //    var role = HttpContext.Session.GetString("UserRole");
        //    if (role != "Admin")
        //        return RedirectToAction("Login", "Admin");

        //    // Example: Count of teachers and projects
        //    var model = new AdminDashboardViewModel
        //    {
        //        TotalTeachers = _context.Teachers.Count(),
        //        TotalProjects = _context.Projects.Count(),
        //        RecentTeachers = _context.Teachers
        //            .OrderByDescending(t => t.Id)
        //            .Take(5)
        //            .ToList()
        //    };

        //    return View(model);
        //}
        
        public IActionResult Dashboard()
        {
            // Only admin can access
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return RedirectToAction("Login", "Admin");

            ViewBag.Layout = "~/Views/Shared/_AdminLayout.cshtml";
            ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Admin";

            var model = new AdminDashboardViewModel
            {
                TotalTeachers = _context.Teachers.Count(),
                TotalProjects = _context.Projects.Count(),
                RecentTeachers = _context.Teachers
                    .OrderByDescending(t => t.Id)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
        // ===============================
        // LOGIN (Teacher Table)
        // ===============================
        //[HttpGet]
        //public IActionResult Login()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> Login(LoginModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    // ✅ Find user in Teacher table using async
        //    var user = await _context.Teachers
        //        .FirstOrDefaultAsync(x => x.Email == model.Email && x.PasswordHash == model.Password);

        //    if (user == null)
        //    {
        //        ModelState.AddModelError("", "Invalid email or password");
        //        return View(model);
        //    }

        //    // ✅ Save necessary session values
        //    HttpContext.Session.SetString("UserId", user.Id.ToString());
        //    HttpContext.Session.SetString("UserName", user.FullName);
        //    HttpContext.Session.SetString("UserEmail", user.Email);
        //    HttpContext.Session.SetString("UserRole", user.Role); // "Admin" or "Teacher"

        //    // ✅ Redirect based on role
        //    if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return RedirectToAction("Dashboard", "Admin");
        //    }
        //    else // Teacher
        //    {
        //        return RedirectToAction("Dashboard", "Teacher");
        //    }
        //}

        //[HttpPost]
        //public IActionResult Logout()
        //{
        //    HttpContext.Session.Clear();
        //    return RedirectToAction("Login");
        //}

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Teachers
                .FirstOrDefault(x =>
                    x.Email == model.Email &&
                    x.PasswordHash == model.Password 
                    /*x.IsDeleted == false*/);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserInitial", user.FullName.Substring(0, 1));
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);

            if (user.Role == "Teacher")
            {
                HttpContext.Session.SetInt32("TeacherId", user.Id);
            }

            return user.Role == "Admin"
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Dashboard", "Teacher");
        }


        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> DownloadRequests()
        {
            var requests = await _context.DownloadRequests
                .Include(r => r.StudentPk)
                    .ThenInclude(s => s.AcademicYearPk)   
                .Include(r => r.ProjectFilePk)
                    .ThenInclude(f => f.ProjectPk)
                        .ThenInclude(p => p.CompanyPk)
                .Include(r => r.DownloadTransactions)   
                //.Where(r => r.IsApproved == null)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.DownloadRequests
                .FirstOrDefaultAsync(r => r.DownloadRequestPkId == id);

            if (request == null)
                return NotFound();

            request.IsApproved = true;
            request.IsBlocked = false;
            request.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("DownloadRequests");
        }
        public async Task<IActionResult> BlockRequest(int id)
        {
            var request = await _context.DownloadRequests
                .FirstOrDefaultAsync(r => r.DownloadRequestPkId == id);

            if (request == null)
                return NotFound();

            request.IsApproved = false;
            request.IsBlocked = true;
            request.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("DownloadRequests");
        }


        // ===============================
        // CHANGE PASSWORD (Teacher Table)
        // ===============================
        [HttpGet]
        //[Authorize(Roles = "Admin,Teacher")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.Teachers.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login");

            if (user.PasswordHash != model.OldPassword)
            {
                ModelState.AddModelError("", "Old password is incorrect.");
                return View(model);
            }

            // Set new password
            user.PasswordHash = model.NewPassword;
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Password changed successfully.";

            return RedirectToAction("ChangePassword");
        }


        [HttpGet]
        public async Task<IActionResult> MyProfile(bool isEditMode = false)
        {
            // Get session values
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Admin"); // redirect to login if session missing

            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Admin");

            var user = await _context.Teachers.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Admin");

            var model = new MyProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                IsEditMode = isEditMode
            };

            ViewBag.Role = userRole; // Pass role to view

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(MyProfileViewModel model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(userRole))
                return RedirectToAction("Login", "Admin");

            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Admin");

            var user = await _context.Teachers.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Admin");

            if (!ModelState.IsValid)
            {
                model.IsEditMode = true;
                ViewBag.Role = userRole;
                return View(model);
            }

            // Update FullName
            user.FullName = model.FullName;
            HttpContext.Session.SetString("UserName", model.FullName);

            // Password change
            if (!string.IsNullOrEmpty(model.OldPassword) || !string.IsNullOrEmpty(model.NewPassword))
            {
                if (model.OldPassword != user.PasswordHash)
                {
                    ModelState.AddModelError("OldPassword", "Old password is incorrect.");
                    model.IsEditMode = true;
                    ViewBag.Role = userRole;
                    return View(model);
                }

                user.PasswordHash = model.NewPassword;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully";

            return RedirectToAction(nameof(MyProfile));
        }




        [HttpPost]
        public IActionResult ToggleEditMode()
        {
            return RedirectToAction(nameof(MyProfile), new { isEditMode = true });
        }
    }
}



