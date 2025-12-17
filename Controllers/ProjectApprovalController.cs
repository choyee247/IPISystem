using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem.Data;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.ViewModels;

namespace ProjectManagementSystem.Controllers
{
    public class ProjectApprovalController : Controller
    {
        private readonly PMSDbContext _context;

        public ProjectApprovalController(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
          string statusFilter = "all",
          string searchString = "",
          DateTime? fromDate = null,
          DateTime? toDate = null,
          int pageNumber = 1)
        {
            const int pageSize = 15;

            var query = _context.Projects
                .Include(p => p.CompanyPk)
                .Include(p => p.ProjectTypePk)
                .Include(p => p.ProjectMembers) 
                    .ThenInclude(pm => pm.StudentPk) 
                .Where(p => p.IsDeleted == null || p.IsDeleted == false);

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
                //    Projects = await query
                //.OrderByDescending(p => p.ProjectSubmittedDate)
                //.Skip((pageNumber - 1) * pageSize)
                //.Take(pageSize)
                //.ToListAsync(),
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
        //    var project = await _context.Projects.FindAsync(id);
        //    if (project == null)
        //        return Json(new { success = false, message = "Project not found." });

        //    project.Status = "Approved";
        //    project.ApprovedDate = DateTime.Now;
        //    project.AdminComment = null;

        //    await _context.SaveChangesAsync();
        //    // ✅ Student Notification
        //    var leader = project.ProjectMembers.FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;
        //    if (leader != null)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = leader.StudentPkId,
        //            Title = "Project Approved",
        //            Message = $"Your project '{project.ProjectName}' has been approved by the teacher.",
        //            NotificationType = "Response",
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
            // ✅ 1. Check Session Role
            var role = HttpContext.Session.GetString("UserRole");
            var teacherIdStr = HttpContext.Session.GetString("UserId");

            if (role != "Teacher" || string.IsNullOrEmpty(teacherIdStr))
            {
                return Json(new { success = false, message = "Only teacher can approve." });
            }

            int teacherId = int.Parse(teacherIdStr);

            // ✅ 2. Load Project
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null)
                return Json(new { success = false, message = "Project not found." });

            // ✅ 3. Update Project Table
            project.Status = "Approved";
            project.IsApprovedByTeacher = true;
            project.ApprovedDate = DateTime.Now;
            project.TeacherId = teacherId;      // ⭐ IMPORTANT
            project.AdminComment = null;

            await _context.SaveChangesAsync();

            // ✅ 4. Find Leader
            var leader = project.ProjectMembers
                .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

            // ✅ 5. Create Notification
            if (leader != null)
            {
                var notification = new Notification
                {
                    UserId = leader.StudentPkId,   // Student
                    TeacherId = teacherId,         // ⭐ Teacher who approved
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
            }

            return Json(new { success = true });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Approve([FromBody] int id)
        //{
        //    var project = await _context.Projects
        //        .Include(p => p.ProjectMembers)
        //            .ThenInclude(pm => pm.StudentPk)
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == id);

        //    if (project == null)
        //        return Json(new { success = false, message = "Project not found." });

        //    project.Status = "Approved";
        //    project.ApprovedDate = DateTime.Now;
        //    project.AdminComment = null;

        //    await _context.SaveChangesAsync();

        //    // FIND LEADER
        //    var leader = project.ProjectMembers
        //        .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

        //    if (leader != null)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = leader.StudentPkId,
        //            Title = "Project Approved",
        //            Message = $"Your project '{project.ProjectName}' has been approved by the teacher.",
        //            NotificationType = "Response",
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
            // ✅ 1. Check Teacher Session
            var role = HttpContext.Session.GetString("UserRole");
            var teacherIdStr = HttpContext.Session.GetString("UserId");

            if (role != "Teacher" || string.IsNullOrEmpty(teacherIdStr))
            {
                return Json(new { success = false, message = "Only teacher can reject." });
            }

            int teacherId = int.Parse(teacherIdStr);

            // ✅ 2. Load Project
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == model.Id);

            if (project == null)
                return Json(new { success = false, message = "Project not found." });

            if (string.IsNullOrWhiteSpace(model.Reason))
                return Json(new { success = false, message = "Rejection reason is required." });

            // ✅ 3. Update Project (Rejected)
            project.Status = "Rejected";
            project.IsApprovedByTeacher = false;
            project.RejectedDate = DateTime.Now;
            project.ApprovedDate = null;
            project.AdminComment = model.Reason;
            project.TeacherId = teacherId;        // ⭐ IMPORTANT

            await _context.SaveChangesAsync();

            // ✅ 4. Find Leader
            var leader = project.ProjectMembers
                .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

            // ✅ 5. Create Notification
            if (leader != null)
            {
                var notification = new Notification
                {
                    UserId = leader.StudentPkId,     // Student
                    TeacherId = teacherId,           // ⭐ Teacher who rejected
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
            }

            return Json(new { success = true });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Reject([FromBody] RejectModel model)
        //{
        //    var project = await _context.Projects
        //        .Include(p => p.ProjectMembers)
        //            .ThenInclude(pm => pm.StudentPk)
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == model.Id);

        //    if (project == null)
        //        return Json(new { success = false, message = "Project not found." });

        //    if (string.IsNullOrWhiteSpace(model.Reason))
        //        return Json(new { success = false, message = "Rejection reason is required." });

        //    project.Status = "Rejected";
        //    project.AdminComment = model.Reason;
        //    project.ApprovedDate = null;

        //    await _context.SaveChangesAsync();

        //    // FIND Leader
        //    var leader = project.ProjectMembers
        //        .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;

        //    if (leader != null)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = leader.StudentPkId,
        //            Title = "Project Rejected",
        //            Message = $"Your project '{project.ProjectName}' has been rejected. Reason: {model.Reason}",
        //            NotificationType = "Response",
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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Reject([FromBody] RejectModel model)
        //{
        //    var project = await _context.Projects.FindAsync(model.Id);
        //    if (project == null)
        //        return Json(new { success = false, message = "Project not found." });

        //    if (string.IsNullOrWhiteSpace(model.Reason))
        //        return Json(new { success = false, message = "Rejection reason is required." });

        //    project.Status = "Rejected";
        //    project.AdminComment = model.Reason;
        //    project.ApprovedDate = null;

        //    await _context.SaveChangesAsync();
        //    // ✅ Student Notification
        //    var leader = project.ProjectMembers.FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk;
        //    if (leader != null)
        //    {
        //        var notification = new Notification
        //        {
        //            UserId = leader.StudentPkId,
        //            Title = "Project Rejected",
        //            Message = $"Your project '{project.ProjectName}' has been rejected. Reason: {model.Reason}",
        //            NotificationType = "Response",
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