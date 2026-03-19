using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

//using ProjectManagementSystem.Data;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Services;
using ProjectManagementSystem.ViewModels;

namespace ProjectManagementSystem.Controllers
{
    public class ProjectApprovalController : Controller
    {
        private readonly PMSDbContext _context;
        private readonly IEmailService _emailService;
        public ProjectApprovalController(PMSDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(
      string statusFilter = "all",
      string searchString = "",
      DateTime? fromDate = null,
      DateTime? toDate = null,
      int pageNumber = 1)
        {
            const int pageSize = 15;

            // Get logged-in user info
            var userRole = HttpContext.Session.GetString("UserRole");
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdString))
                userId = int.Parse(userIdString);

            var query = _context.Projects
                .Include(p => p.CompanyPk)
                .Include(p => p.ProjectTypePk)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .Where(p => (p.IsDeleted == null || p.IsDeleted == false) && p.Status != "Draft");
            // Filter by role
            if (userRole == "Teacher")
            {
                // Show only projects supervised by this teacher
                query = query.Where(p => p.TeacherId == userId);
            }
            else if (userRole == "Admin")
            {
                // Admin sees all projects
            }
            else
            {
                // Other roles cannot access
                return RedirectToAction("Login", "Teacher");
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    p.ProjectName.Contains(searchString) ||
                    p.CreatedBy.Contains(searchString) ||
                    p.ProjectMembers.Any(pm =>
                    pm.StudentPk.StudentName.Contains(searchString))
                );
            }

            // Apply date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.ProjectSubmittedDate >= fromDate);
            }
            if (toDate.HasValue)
            {
                query = query.Where(p => p.ProjectSubmittedDate <= toDate.Value.AddDays(1));
            }

            // Apply status filter
            if (statusFilter != "all")
            {
                query = query.Where(p => p.Status == statusFilter);
            }

            // Get counts and paginated results
            var totalCount = await query.CountAsync();
            var projects = await query
                .OrderByDescending(p => p.ProjectSubmittedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new ProjectApprovalViewModel
            {
                Projects = projects,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                StatusFilter = statusFilter,
                SearchString = searchString,
                FromDate = fromDate,
                ToDate = toDate,
                PageTitle = statusFilter == "all" ? "All Projects" : $"{statusFilter} Projects",
                PendingCount = await _context.Projects.CountAsync(p => p.Status == "Pending" && (p.IsDeleted == null || p.IsDeleted == false)),
                ApprovedCount = await _context.Projects.CountAsync(p => p.Status == "Approved" && (p.IsDeleted == null || p.IsDeleted == false)),
                RejectedCount = await _context.Projects.CountAsync(p => p.Status == "Rejected" && (p.IsDeleted == null || p.IsDeleted == false))
            };

            ViewBag.PageTitle = model.PageTitle;
            ViewBag.CurrentFilter = model.StatusFilter;
            ViewBag.SearchString = model.SearchString;

            return View(model);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Approve([FromBody] int id)
        //{
        //    var role = HttpContext.Session.GetString("UserRole");
        //    var userIdStr = HttpContext.Session.GetString("UserId");

        //    // ✅ Allow Teacher OR Admin
        //    if ((role != "Teacher" && role != "Admin") || string.IsNullOrEmpty(userIdStr))
        //    {
        //        return Json(new { success = false, message = "Only teacher or admin can approve." });
        //    }

        //    int userId = int.Parse(userIdStr);

        //    var project = await _context.Projects
        //        .Include(p => p.ProjectMembers)
        //            .ThenInclude(pm => pm.StudentPk)
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == id);

        //    if (project == null)
        //        return Json(new { success = false, message = "Project not found." });

        //    project.Status = "Approved";
        //    project.IsApprovedByTeacher = true;
        //    project.ApprovedDate = DateTime.Now;

        //    project.TeacherId = userId;
        //    project.AdminComment = null;

        //    await _context.SaveChangesAsync();

        //    var leader = project.ProjectMembers
        //        .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

        //    if (leader != null)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = leader.StudentPkId,
        //            TeacherId = userId,
        //            Title = "Project Approved",
        //            Message = $"Your project '{project.ProjectName}' has been approved.",
        //            NotificationType = "ProjectStatus",
        //            ProjectPkId = project.ProjectPkId,
        //            CreatedAt = DateTime.Now,
        //            IsRead = false,
        //            IsDeleted = false
        //        };

        //        _context.Notifications.Add(notification);
        //        await _context.SaveChangesAsync();

        //    }

        //    return Json(new { success = true });
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve([FromBody] int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userIdStr = HttpContext.Session.GetString("UserId");

            // ✅ Only Teacher or Admin allowed
            if ((role != "Teacher" && role != "Admin") || string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "Only teacher or admin can approve." });
            }

            int userId = int.Parse(userIdStr);

            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                        .ThenInclude(s => s.EmailPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null)
                return Json(new { success = false, message = "Project not found." });

            // ✅ Update Project Status
            project.Status = "Approved";
            project.IsApprovedByTeacher = true;
            project.ApprovedDate = DateTime.Now;
            project.TeacherId = userId;
            project.AdminComment = null;

            await _context.SaveChangesAsync();

            // ✅ Get Leader
            var leader = project.ProjectMembers
                .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

            if (leader != null)
            {
                // 🔔 1️⃣ SYSTEM NOTIFICATION (Always Save)
                var notification = new Notification
                {
                    UserId = leader.StudentPkId,
                    TeacherId = userId,
                    Title = "Project Approved",
                    Message = $"Your project '{project.ProjectName}' has been approved.",
                    NotificationType = "ProjectStatus",
                    ProjectPkId = project.ProjectPkId,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    IsDeleted = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var teacherName = HttpContext.Session.GetString("UserName") ?? "Your Teacher"; // Teacher name session ကနေယူ

                if (leader.IsEmailSubscribed
                    && leader.EmailPk != null
                    && !string.IsNullOrEmpty(leader.EmailPk.EmailAddress))
                {
                    var emailBody = $@"
Hello {leader.StudentName},

Your project '{project.ProjectName}' has been approved by {teacherName}.

Thank you for your effort and congratulations on your project approval!

Best regards,
Project Management System
";

                    await _emailService.SendEmailAsync(
                        leader.EmailPk.EmailAddress,
                        "Project Approved",
                        emailBody
                    );
                }
            }

            return Json(new { success = true });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Reject([FromBody] RejectModel model)
        //{
        //    var role = HttpContext.Session.GetString("UserRole");
        //    var userIdStr = HttpContext.Session.GetString("UserId");

        //    // ✅ Allow Teacher OR Admin
        //    if ((role != "Teacher" && role != "Admin") || string.IsNullOrEmpty(userIdStr))
        //    {
        //        return Json(new { success = false, message = "Only teacher or admin can reject." });
        //    }

        //    int userId = int.Parse(userIdStr);

        //    var project = await _context.Projects
        //        .Include(p => p.ProjectMembers)
        //            .ThenInclude(pm => pm.StudentPk)
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == model.Id);

        //    if (project == null)
        //        return Json(new { success = false, message = "Project not found." });

        //    if (string.IsNullOrWhiteSpace(model.Reason))
        //        return Json(new { success = false, message = "Rejection reason is required." });

        //    project.Status = "Rejected";
        //    project.IsApprovedByTeacher = false;
        //    project.RejectedDate = DateTime.Now;
        //    project.ApprovedDate = null;
        //    project.AdminComment = model.Reason;

        //    // ✅ Save who rejected (Teacher OR Admin)
        //    project.TeacherId = userId;

        //    await _context.SaveChangesAsync();

        //    var leader = project.ProjectMembers
        //        .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

        //    if (leader != null)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = leader.StudentPkId,
        //            TeacherId = userId,
        //            Title = "Project Rejected",
        //            Message = $"Your project '{project.ProjectName}' has been rejected.\nReason: {model.Reason}",
        //            NotificationType = "ProjectStatus",
        //            ProjectPkId = project.ProjectPkId,
        //            CreatedAt = DateTime.Now,
        //            IsRead = false,
        //            IsDeleted = false
        //        };

        //        _context.Notifications.Add(notification);
        //        await _context.SaveChangesAsync();
        //    }

        //    return Json(new { success = true });
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject([FromBody] RejectModel model)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userIdStr = HttpContext.Session.GetString("UserId");
            var teacherName = HttpContext.Session.GetString("UserName") ?? "Your Teacher";

            // ✅ Only Teacher or Admin
            if ((role != "Teacher" && role != "Admin") || string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "Only teacher or admin can reject." });
            }

            int userId = int.Parse(userIdStr);

            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                        .ThenInclude(s => s.EmailPk) // 👈 Email navigation include လုပ်ထား
                .FirstOrDefaultAsync(p => p.ProjectPkId == model.Id);

            if (project == null)
                return Json(new { success = false, message = "Project not found." });

            if (string.IsNullOrWhiteSpace(model.Reason))
                return Json(new { success = false, message = "Rejection reason is required." });

            // ✅ Update project status
            project.Status = "Rejected";
            project.IsApprovedByTeacher = false;
            project.RejectedDate = DateTime.Now;
            project.ApprovedDate = null;
            project.AdminComment = model.Reason;
            project.TeacherId = userId;

            await _context.SaveChangesAsync();

            // ✅ Get leader
            var leaderMember = project.ProjectMembers
                .FirstOrDefault(pm => pm.Role == "Leader");

            if (leaderMember != null && leaderMember.StudentPk != null)
            {
                var leader = leaderMember.StudentPk;

                // 🔔 System Notification
                var notification = new Notification
                {
                    UserId = leader.StudentPkId,
                    TeacherId = userId,
                    Title = "Project Rejected",
                    Message = $"Your project '{project.ProjectName}' has been rejected.\nReason: {model.Reason}",
                    NotificationType = "ProjectStatus",
                    ProjectPkId = project.ProjectPkId,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    IsDeleted = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 📧 Email Notification (only if subscribed & Email exists)
                if (leader.IsEmailSubscribed
                    && leader.EmailPk != null
                    && !string.IsNullOrEmpty(leader.EmailPk.EmailAddress))
                {
                    var emailBody = $@"
Hello {leader.StudentName},

Your project '{project.ProjectName}' has been rejected by {teacherName}.

Reason: {model.Reason}

Please review and make the necessary changes.

Thank you,
Project Management System
";

                    try
                    {
                        await _emailService.SendEmailAsync(
                            leader.EmailPk.EmailAddress,
                            "Project Rejected",
                            emailBody
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Email sending failed: " + ex.Message);
                    }
                }
            }

            return Json(new { success = true });
        }
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null) return NotFound();

            return View(project);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Cancel(
        //    int ProjectPkId,
        //    string Reason,
        //    List<IFormFile> Files)
        //{
        //    var role = HttpContext.Session.GetString("UserRole");

        //    if (role != "Teacher" && role != "Admin")
        //        return Unauthorized();

        //    var project = await _context.Projects
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == ProjectPkId);

        //    if (project == null) return NotFound();

        //    // ⭐ Change Status
        //    project.Status = "Cancelled";
        //    project.AdminComment = Reason;
        //    project.ApprovedDate = null;
        //    project.RejectedDate = DateTime.Now;

        //    await _context.SaveChangesAsync();

        //    // ⭐ Save Attachments
        //    if (Files != null && Files.Any())
        //    {
        //        var folder = Path.Combine("wwwroot/uploads/cancel");
        //        Directory.CreateDirectory(folder);

        //        foreach (var file in Files)
        //        {
        //            var fileName = Guid.NewGuid() +
        //                           Path.GetExtension(file.FileName);

        //            var path = Path.Combine(folder, fileName);

        //            using var stream =
        //                new FileStream(path, FileMode.Create);

        //            await file.CopyToAsync(stream);

        //            _context.ProjectFiles.Add(new ProjectFile
        //            {
        //                ProjectPkId = ProjectPkId,
        //                FilePath = "/uploads/cancel/" + fileName,
        //                FileType = "CancelReason",
        //                FileSize = file.Length,
        //                UploadedAt = DateTime.Now
        //            });
        //        }

        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction("Index");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
    int ProjectPkId,
    string Reason,
    List<IFormFile> Files)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Teacher" && role != "Admin")
                return Unauthorized();

            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                        .ThenInclude(s => s.EmailPk) // 👈 Email navigation include
                .FirstOrDefaultAsync(p => p.ProjectPkId == ProjectPkId);

            if (project == null)
                return NotFound();

            // ⭐ Get TeacherId & Name from Session
            int? teacherId = HttpContext.Session.GetInt32("TeacherId");
            string teacherName = HttpContext.Session.GetString("UserName") ?? "Your Teacher";

            // ⭐ Update Project Status
            project.Status = "Cancelled";
            project.AdminComment = Reason;
            project.ApprovedDate = null;
            project.RejectedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ⭐ Get ALL project members (including owner)
            var memberIds = project.ProjectMembers
                .Select(pm => pm.StudentPk)
                .Where(s => s != null)
                .ToList();

            if (!memberIds.Any(s => s.StudentPkId == project.StudentPkId))
            {
                var owner = await _context.Students.FindAsync(project.StudentPkId);
                if (owner != null)
                    memberIds.Add(owner);
            }

            foreach (var student in memberIds)
            {
                // 🔔 System Notification
                _context.Notifications.Add(new Notification
                {
                    UserId = student.StudentPkId,
                    TeacherId = teacherId,
                    Title = "Project Cancelled",
                    Message = $"Your project \"{project.ProjectName}\" has been cancelled." +
                              (string.IsNullOrEmpty(Reason) ? "" : $" Reason: {Reason}"),
                    NotificationType = "ProjectStatus",
                    ProjectPkId = project.ProjectPkId,
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    IsDeleted = false
                });

                // 📧 Email Notification (only if subscribed)
                if (student.IsEmailSubscribed
                    && student.EmailPk != null
                    && !string.IsNullOrEmpty(student.EmailPk.EmailAddress))
                {
                    var emailBody = $@"
Hello {student.StudentName},

Your project '{project.ProjectName}' has been cancelled by {teacherName}.
{(string.IsNullOrEmpty(Reason) ? "" : $"Reason: {Reason}\n")}

Please review your project and make any necessary improvements. We appreciate your effort and dedication.

Thank you for your understanding.

Best regards,
Project Management System
";
                    try
                    {
                        await _emailService.SendEmailAsync(
                            student.EmailPk.EmailAddress,
                            "Project Cancelled",
                            emailBody
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Email sending failed: " + ex.Message);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // ⭐ Save Attachments
            if (Files != null && Files.Any())
            {
                var folder = Path.Combine("wwwroot/uploads/cancel");
                Directory.CreateDirectory(folder);

                foreach (var file in Files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var path = Path.Combine(folder, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.ProjectFiles.Add(new ProjectFile
                    {
                        ProjectPkId = ProjectPkId,
                        FilePath =  fileName,
                        FileType = "Cancel",
                        FileSize = file.Length,
                        UploadedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.CompanyPk)
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.FrameworkPk)
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.IsPending = project.Status == "Pending";
            ViewBag.StatusMessage = TempData["StatusMessage"];
            ViewBag.IsSuccess = TempData["IsSuccess"];

            return View(project);
        }

        public class RejectModel
        {
            public int Id { get; set; }
            public string Reason { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Export([FromBody] ProjectApprovalViewModel model)
        {
            try
            {
                // Get all projects based on filters, ignoring pagination
                var projects = await _context.Projects
                    .Where(p => model.StatusFilter == "all" || p.Status == model.StatusFilter)
                    .Where(p => string.IsNullOrEmpty(model.SearchString) ||
                           p.ProjectName.Contains(model.SearchString) ||
                           p.CreatedBy.Contains(model.SearchString))
                    .Where(p => !model.FromDate.HasValue || p.ProjectSubmittedDate >= model.FromDate)
                    .Where(p => !model.ToDate.HasValue || p.ProjectSubmittedDate <= model.ToDate)
                    .Select(p => new
                    {
                        projectName = p.ProjectName,
                        createdBy = p.CreatedBy,
                        projectSubmittedDate = p.ProjectSubmittedDate,
                        status = p.Status,
                        description = p.Description,
                       
                    })
                    .ToListAsync();

                return Json(new { success = true, projects = projects });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // In ProjectApprovalController.cs
        [HttpGet]
        public IActionResult SharedView(string statusFilter)
        {
            // This ensures the Index action can handle requests from both controllers
            return RedirectToAction("Index", new { statusFilter });
        }

        // Add to ProjectApprovalController.cs
        public async Task<IActionResult> AllProjects()
        {
            var model = new ProjectApprovalViewModel
            {
                Projects = await _context.Projects
                    .Include(p => p.CompanyPk)
                    .Include(p => p.ProjectTypePk)
                    .Where(p => p.IsDeleted == null || p.IsDeleted == false)
                    .OrderByDescending(p => p.ProjectSubmittedDate)
                    .ToListAsync(),
                PageTitle = "All Projects"
            };
            return View("Index", model);
        }

        public async Task<IActionResult> ProjectsByDate(string date)
        {
            if (!DateTime.TryParse(date, out var filterDate))
            {
                filterDate = DateTime.Today;
            }

            var model = new ProjectApprovalViewModel
            {
                Projects = await _context.Projects
                    .Include(p => p.CompanyPk)
                    .Include(p => p.ProjectTypePk)
                    .Where(p => p.ProjectSubmittedDate.HasValue &&
                           p.ProjectSubmittedDate.Value.Date == filterDate.Date)
                    .OrderByDescending(p => p.ProjectSubmittedDate)
                    .ToListAsync(),
                PageTitle = $"Projects Submitted on {filterDate.ToShortDateString()}"
            };
            return View("Index", model);
        }

         
    }
}