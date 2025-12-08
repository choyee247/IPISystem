// Controllers/TeacherController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.ViewModels;
using System.Diagnostics;

namespace ProjectManagementSystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly PMSDbContext _context;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(PMSDbContext context, ILogger<TeacherController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //[Authorize(Roles = "Admin,Teacher")]
        //public async Task<IActionResult> Dashboard()
        //{
        //    try
        //    {
        //        var date = DateTime.Now.Date;
        //        var submissionStats = await GetSubmissionStatsAsync(date);

        //        var model = new DBModels.TeacherDashboardViewModel
        //        {
        //            PendingProjectsCount = await _context.Projects
        //                .Where(p => p.Status == "Pending" && (p.IsDeleted == null || !p.IsDeleted.Value))
        //                .CountAsync(),

        //            Announcements = await GetRecentAnnouncementsAsync(),

        //            RecentSubmitters = await GetRecentSubmittersAsync(),

        //            TotalProjects = await _context.Projects
        //                .Where(p => p.IsDeleted == null || !p.IsDeleted.Value)
        //                .CountAsync(),

        //            SubmissionStats = submissionStats
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error loading teacher dashboard");
        //        TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
        //        return RedirectToAction("Error", "Home");
        //    }
        //}
        //public async Task<IActionResult> Dashboard()
        //{
        //    try
        //    {
        //        var date = DateTime.Now.Date;
        //        var submissionStats = await GetSubmissionStatsAsync(date);

        //        var role = User.IsInRole("Admin") ? "Admin" : "Teacher";

        //        var model = new DBModels.TeacherDashboardViewModel
        //        {
        //            PendingProjectsCount = await _context.Projects
        //                .Where(p => p.Status == "Pending" && (p.IsDeleted == null || !p.IsDeleted.Value))
        //                .CountAsync(),

        //            Announcements = await GetRecentAnnouncementsAsync(),

        //            RecentSubmitters = await GetRecentSubmittersAsync(),

        //            TotalProjects = await _context.Projects
        //                .Where(p => p.IsDeleted == null || !p.IsDeleted.Value)
        //                .CountAsync(),

        //            SubmissionStats = submissionStats
        //        };

        //        // Pass role and full name to view
        //        ViewBag.Role = role;
        //        ViewBag.FullName = User.Identity.Name;

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error loading teacher dashboard");
        //        TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
        //        return RedirectToAction("Error", "Home");
        //    }
        //}

        // ---------------------- INDEX ----------------------
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Teachers.ToListAsync();
            return View(teachers);
        }

        // ---------------------- CREATE ----------------------
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Teacher model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.CreatedDate = DateTime.Now;
            _context.Teachers.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ---------------------- EDIT ----------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            return View(teacher);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Teacher model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            teacher.FullName = model.FullName;
            teacher.Email = model.Email;
            teacher.PasswordHash = model.PasswordHash;
            teacher.Role = model.Role;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ---------------------- DELETE ----------------------
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            return View(teacher);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var date = DateTime.Now.Date;
                var submissionStats = await GetSubmissionStatsAsync(date);

                var model = new DBModels.TeacherDashboardViewModel
                {
                    PendingProjectsCount = await _context.Projects
                        .Where(p => p.Status == "Pending" && (p.IsDeleted == null || !p.IsDeleted.Value))
                        .CountAsync(),

                    Announcements = await GetRecentAnnouncementsAsync(),

                    RecentSubmitters = await GetRecentSubmittersAsync(),

                    TotalProjects = await _context.Projects
                        .Where(p => p.IsDeleted == null || !p.IsDeleted.Value)
                        .CountAsync(),

                    SubmissionStats = submissionStats
                };

                // Teacher layout (default)
                ViewBag.Layout = "_TeacherLayout";
                ViewBag.FullName = User.Identity.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Error", "Home");
            }
        }

        

        [Authorize(Roles = "Admin,Teacher")]
        private async Task<List<ViewModels.SubmissionStat>> GetSubmissionStatsAsync(DateTime currentDate)
        {
            try
            {
                var stats = new List<ViewModels.SubmissionStat>();
                var dateRange = Enumerable.Range(0, 7)
                    .Select(i => currentDate.AddDays(-i).Date)
                    .ToList();

                var actualCounts = await _context.Projects
                    .Where(p => p.ProjectSubmittedDate.HasValue &&
                           dateRange.Contains(p.ProjectSubmittedDate.Value.Date))
                    .GroupBy(p => p.ProjectSubmittedDate.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                foreach (var date in dateRange.OrderBy(d => d))
                {
                    stats.Add(new ViewModels.SubmissionStat
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Count = actualCounts.FirstOrDefault(a => a.Date == date)?.Count ?? 0
                    });
                }
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission stats");
                return Enumerable.Range(0, 7)
                    .Select(i => new ViewModels.SubmissionStat
                    {
                        Date = currentDate.AddDays(-i).ToString("yyyy-MM-dd"),
                        Count = 0
                    })
                    .ToList();
            }
        }
        [Authorize(Roles = "Admin,Teacher")]
        private async Task<List<DBModels.Announcement>> GetRecentAnnouncementsAsync()
        {
            try
            {
                return await _context.Announcements
                    .Where(a => a.IsActive == true) // Only get manually activated announcements
                    .OrderByDescending(a => a.CreatedDate)
                    .Take(1) // Only get the most recent active announcement
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent announcements");
                return new List<DBModels.Announcement>();
            }
        }
        [Authorize(Roles = "Admin,Teacher")]
        private async Task<List<StudentSubmission>> GetRecentSubmittersAsync()
        {
            try
            {
                return await _context.Projects
                    .Include(p => p.SubmittedByStudentPk)
                    .Where(p => p.Status == "Submitted" &&
                           p.SubmittedByStudentPk != null &&
                           p.ProjectSubmittedDate.HasValue)
                    .OrderByDescending(p => p.ProjectSubmittedDate)
                    .Take(5)
                    .Select(p => new StudentSubmission
                    {
                        StudentName = p.SubmittedByStudentPk.StudentName,
                        ProjectName = p.ProjectName,
                        SubmissionDate = p.ProjectSubmittedDate.Value
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent submitters");
                return new List<StudentSubmission>();
            }
        }
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> AllProjects()
        {
            return RedirectToAction("Index", "ProjectApproval", new { statusFilter = "all" });
        }
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> PendingProjects()
        {
            return RedirectToAction("Index", "ProjectApproval", new { statusFilter = "Pending" });
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature?.Error, "Error occurred in TeacherController");

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> ProjectsByDate(string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out var filterDate))
                {
                    TempData["ErrorMessage"] = "Invalid date format";
                    return RedirectToAction("Dashboard");
                }

                var Projects = await _context.Projects
                    .Include(p => p.CompanyPk)
                    .Include(p => p.ProjectTypePk)
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(pm => pm.StudentPk)
                    .Where(p => p.ProjectSubmittedDate.HasValue &&
                           p.ProjectSubmittedDate.Value.Date == filterDate.Date &&
                           (p.IsDeleted == null || p.IsDeleted.Value == false))
                    .OrderByDescending(p => p.ProjectSubmittedDate)
                    .ToListAsync();


                return View("~/Views/ProjectApproval/Index.cshtml", new ProjectApprovalViewModel
                {
                    Projects = Projects,
                    PageTitle = $"Projects Submitted on {filterDate.ToString("MMM dd, yyyy")}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by date");
                TempData["ErrorMessage"] = "Error loading projects by date";
                return RedirectToAction("Dashboard");
            }
        }
    }
}
