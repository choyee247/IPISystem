//// Controllers/AdminController.cs
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

////using Microsoft.EntityFrameworkCore;
////using ProjectManagementSystem.Data;
//using ProjectManagementSystem.DBModels;
//using ProjectManagementSystem.Models;
//using ProjectManagementSystem.Services.Interface;
//using ProjectManagementSystem.ViewModels;
//using System.Security.Claims;
//using System.Threading.Tasks;


//namespace ProjectManagementSystem.Controllers
//{
//    public class AdminController : Controller
//    {
//        private readonly SignInManager<ApplicationUser> _signInManager;
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly IActivityLogger _activityLogger;
//        private readonly PMSDbContext _context;

//        public AdminController(
//            SignInManager<ApplicationUser> signInManager,
//            UserManager<ApplicationUser> userManager,
//            IActivityLogger activityLogger,
//            PMSDbContext context)
//        {
//            _signInManager = signInManager;
//            _userManager = userManager;
//            _activityLogger = activityLogger;
//            _context = context;
//        }




//        [HttpGet]
//        public IActionResult Login()
//        {
//            return View();
//        }

//        [HttpPost]
//        public async Task<IActionResult> Login(LoginModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                var user = await _userManager.FindByEmailAsync(model.Email);
//                if (user == null)
//                {
//                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//                    return View(model);
//                }

//                var result = await _signInManager.PasswordSignInAsync(
//                    model.Email,
//                    model.Password,
//                    model.RememberMe,
//                    lockoutOnFailure: false);

//                if (result.Succeeded)
//                {
//                    // Add custom claims
//                    var claims = new List<Claim>
//                    {
//                        new Claim("FullName", user.FullName),
//                        new Claim("Initial", user.FullName.Substring(0, 1).ToUpper())
//                    };
//                    Console.WriteLine(claims);
//                    await _userManager.AddClaimsAsync(user, claims);

//                    await _activityLogger.LogActivityAsync(
//                        user.Id,
//                        "Login",
//                        user.Email,
//                        HttpContext);

//                    if (await _userManager.IsInRoleAsync(user, "Admin"))
//                    {
//                        return RedirectToAction("Dashboard", "Teacher");
//                    }
//                    return RedirectToAction("Dashboard", "Teacher");
//                }

//                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//            }

//            return View(model);
//        }



//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Logout()
//        {
//            Console.WriteLine("Logout action called"); // Debug output

//            var user = await _userManager.GetUserAsync(User);
//            if (user != null)
//            {
//                Console.WriteLine($"Logging out user: {user.UserName}"); // Debug output
//                await _activityLogger.LogActivityAsync(
//                    user.Id,
//                    "Logout",
//                    user.FullName,
//                    HttpContext);
//            }
//            else
//            {
//                Console.WriteLine("No user found to log out"); // Debug output
//            }

//            await _signInManager.SignOutAsync();
//            Console.WriteLine("SignOutAsync completed"); // Debug output

//            return RedirectToAction("Login");
//        }



//        [HttpGet]
//        [Authorize(Roles = "Admin")]
//        public async Task<IActionResult> ActivityLogs()
//        {
//            var logs = await _context.AdminActivityLogs
//                .OrderByDescending(l => l.Timestamp)
//                .ToListAsync();

//            return View(logs);
//        }
//        [HttpGet]
//        [Authorize(Roles = "Admin")]
//        public IActionResult ChangePassword()
//        {
//            return View();
//        }

//        [HttpPost]
//        [Authorize(Roles = "Admin")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
//        {


//            var user = await _userManager.GetUserAsync(User);
//            Console.WriteLine("user null?............................" + (user == null));

//            if (user == null)
//            {
//                return RedirectToAction("Login", "Admin");
//            }
//            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
//            Console.WriteLine("changePasswordResult?............................" + changePasswordResult);

//            if (!changePasswordResult.Succeeded)
//            {
//                foreach (var error in changePasswordResult.Errors)
//                {
//                    ModelState.AddModelError(string.Empty, error.Description);
//                }
//                return View(model);
//            }

//            await _signInManager.RefreshSignInAsync(user);
//            TempData["StatusMessage"] = "Password changed successfully.";
//            return RedirectToAction("ChangePassword");
//        }

//        [HttpGet("Admin/MyProfile")]
//        [Authorize]
//        public async Task<IActionResult> MyProfile(bool isEditMode = false)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            if (user == null)
//            {
//                return NotFound();
//            }

//            var model = new ProjectManagementSystem.DBModels.MyProfileViewModel
//            {
//                FullName = user.FullName,
//                Email = user.Email,
//                IsEditMode = isEditMode
//            };

//            return View(model);
//        }

//        [HttpPost("Admin/MyProfile")]
//        [Authorize]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> MyProfile(MyProfileViewModel model)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            if (user == null) return NotFound();

//            if (!ModelState.IsValid)
//            {
//                model.IsEditMode = true;
//                return View(model);
//            }

//            if (user.FullName != model.FullName)
//            {
//                // Update full name
//                user.FullName = model.FullName;
//                await _userManager.UpdateAsync(user);

//                // Update claims
//                var existingClaims = await _userManager.GetClaimsAsync(user);
//                var fullNameClaim = existingClaims.FirstOrDefault(c => c.Type == "FullName");
//                var initialClaim = existingClaims.FirstOrDefault(c => c.Type == "Initial");

//                if (fullNameClaim != null)
//                {
//                    await _userManager.RemoveClaimAsync(user, fullNameClaim);
//                }
//                if (initialClaim != null)
//                {
//                    await _userManager.RemoveClaimAsync(user, initialClaim);
//                }

//                await _userManager.AddClaimsAsync(user, new List<Claim>
//        {
//            new Claim("FullName", model.FullName),
//            new Claim("Initial", model.FullName.Substring(0, 1).ToUpper())
//        });

//                // Refresh the sign-in to update claims
//                await _signInManager.RefreshSignInAsync(user);
//            }

//            // Handle password change if needed
//            if (!string.IsNullOrWhiteSpace(model.OldPassword) &&
//                !string.IsNullOrWhiteSpace(model.NewPassword))
//            {
//                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
//                if (!result.Succeeded)
//                {
//                    foreach (var error in result.Errors)
//                    {
//                        ModelState.AddModelError(string.Empty, error.Description);
//                    }
//                    model.IsEditMode = true;
//                    return View(model);
//                }

//                user.IsUsingDefaultPassword = false;
//                await _userManager.UpdateAsync(user);
//                await _signInManager.RefreshSignInAsync(user);
//            }

//            TempData["SuccessMessage"] = "Profile updated successfully";
//            return RedirectToAction(nameof(MyProfile));
//        }

//        [HttpPost]
//        [Authorize]
//        [ValidateAntiForgeryToken]
//        public IActionResult ToggleEditMode()
//        {
//            return RedirectToAction(nameof(MyProfile), new { isEditMode = true });
//        }



//    }
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
        private readonly IActivityLogger _activityLogger;

        public AdminController(PMSDbContext context, IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
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
                .FirstOrDefault(x => x.Email == model.Email && x.PasswordHash == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Save to Session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role); // Admin or Teacher

            // Redirect based on role
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
        // ===============================
        // ADMIN ONLY PAGES
        // ===============================
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivityLogs()
        {
            var logs = await _context.AdminActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return View(logs);
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



