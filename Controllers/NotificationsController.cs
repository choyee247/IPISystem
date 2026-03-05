using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagementSystem.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly PMSDbContext _context;

        public NotificationsController(PMSDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> IndexStudent()
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return RedirectToAction("Login", "StudentLogin");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);

            if (student == null)
                return NotFound();

            // ✅ Include all relevant notifications for the student
            var notifications = await _context.Notifications
                .Include(n => n.Announcement)
                .Where(n => n.UserId == student.StudentPkId)
                .Where(n => n.IsDeleted == false || n.IsDeleted == null)
                .Where(n =>
                    n.NotificationType != "ProjectSubmitted" // Skip submissions (teacher/admin only)
                    && (n.NotificationType != "Announcement"
                        || (n.NotificationType == "Announcement" && n.Announcement != null && n.Announcement.IsActive==true))
                // ✅ Keep ProjectStatus (Cancel) notifications
                )
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationViewModel
                {
                    Id = n.NotificationPkId,
                    Title = n.Title ?? "",
                    Message = n.Message ?? "",
                    AnnouncementMessage = n.NotificationType == "Announcement" && n.Announcement != null
                        ? n.Announcement.Message ?? ""
                        : "",
                    CreatedAt = n.CreatedAt ?? DateTime.UtcNow,
                    ProjectId = n.ProjectPkId,
                    ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "",
                    IsRead = n.IsRead,
                    NotificationType = n.NotificationType ?? "",
                    DeadlineStatus = "" // Optional
                })
                .ToListAsync();

            return View(notifications);
        }
        //public IActionResult IndexTeacher()
        //{
        //    var userIdStr = HttpContext.Session.GetString("UserId");
        //    var userRole = HttpContext.Session.GetString("UserRole");

        //    // Redirect only if not logged in
        //    if (string.IsNullOrEmpty(userIdStr))
        //        return RedirectToAction("Login", "Admin"); // Or your actual login controller

        //    int userId = int.Parse(userIdStr);

        //    List<NotificationViewModel> notifications;

        //    if (userRole == "Teacher")
        //    {
        //        // Only teacher-specific notifications
        //        notifications = _context.Notifications
        //            .Include(n => n.ProjectPk)
        //            .Where(n => n.TeacherId == userId && n.UserId == userId)
        //            .Where(n => n.NotificationType == "ProjectSubmitted")
        //            .OrderByDescending(n => n.CreatedAt)
        //            .Select(n => new NotificationViewModel
        //            {
        //                Id = n.NotificationPkId,
        //                Message = n.Message,
        //                CreatedAt = (DateTime)n.CreatedAt,
        //                ProjectId = n.ProjectPkId ?? 0,
        //                ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "No Project",
        //                IsDeleted = n.IsDeleted ?? false,
        //                IsRead = n.IsRead ?? false,
        //            })
        //            .ToList();
        //    }
        //    else if (userRole == "Admin")
        //    {
        //        // Admin sees all notifications
        //        notifications = _context.Notifications
        //            .Include(n => n.ProjectPk)
        //            .Where(n => n.NotificationType == "ProjectSubmitted")
        //            .OrderByDescending(n => n.CreatedAt)
        //            .Select(n => new NotificationViewModel
        //            {
        //                Id = n.NotificationPkId,
        //                Message = n.Message,
        //                CreatedAt = (DateTime)n.CreatedAt,
        //                ProjectId = n.ProjectPkId ?? 0,
        //                ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "No Project",
        //                IsDeleted = n.IsDeleted ?? false,
        //                IsRead = n.IsRead ?? false,
        //            })
        //            .ToList();
        //    }
        //    else
        //    {
        //        notifications = new List<NotificationViewModel>();
        //    }

        //    ViewBag.Notifications = notifications;
        //    ViewBag.UnreadCount = notifications.Count(n => n.IsRead == false);
        //    ViewBag.ReadCount = notifications.Count(n => n.IsRead == true);

        //    return View(notifications);
        //}
        public IActionResult IndexTeacher()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Admin");

            int userId = int.Parse(userIdStr);

            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.NotificationType == "ProjectSubmitted");

            if (role == "Teacher")
            {
                query = query.Where(n => n.ProjectPk.TeacherId == userId);
            }

            // Group by project to avoid duplicates
            var notifications = query
                .GroupBy(n => n.ProjectPkId)
                .Select(g => g.OrderByDescending(n => n.CreatedAt).FirstOrDefault())
                .ToList()
                .Select(n => new NotificationViewModel
                {
                    Id = n.NotificationPkId,
                    Message = n.Message ?? "",
                    CreatedAt = n.CreatedAt ?? DateTime.Now,
                    ProjectId = n.ProjectPkId ?? 0,
                    ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "No Project",
                    IsReadByTeacher = n.IsReadByTeacher ?? false,
                    IsDeletedByTeacher = n.IsDeletedByTeacher ?? false,
                    IsReadByAdmin = n.IsReadByAdmin ?? false,
                    IsDeletedByAdmin = n.IsDeletedByAdmin ?? false
                })
                .Where(n => role == "Teacher" ? !n.IsDeletedByTeacher : !n.IsDeletedByAdmin)
                .ToList();

            ViewBag.Role = role;
            ViewBag.UnreadCount = notifications.Count(n => n.IsUnread(role));
            ViewBag.ReadCount = notifications.Count(n => !n.IsUnread(role));

            return View(notifications);
        }



        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return Json(0);

            var student = await _context.Students
                .Include(s => s.Notifications)
                .FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);

            if (student == null)
                return Json(0);

            var count = student.Notifications.Count(n => n.IsRead==false);
            return Json(count);
        }

     
        public async Task<IActionResult> GetReadCount()
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return Json(0);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);

            if (student == null)
                return Json(0);

            var count = await _context.Notifications
                .Where(n => n.UserId == student.StudentPkId && (n.IsRead == true))
                .CountAsync();

            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return Unauthorized();

            var student = await _context.Students.FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);
            if (student == null)
                return NotFound();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == student.StudentPkId && (n.IsRead == false || n.IsRead == null))
                .ToListAsync();

            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound();

            if (notification.IsRead == false)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
        public async Task<IActionResult> Details(int id)
        {
            var notification = await _context.Notifications
                .Include(n => n.ProjectPk)
                .Include(n => n. Announcement)
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.NotificationPkId == id);

            if (notification == null)
                return NotFound();

            var model = new NotificationViewModel
            {
                Id = notification.NotificationPkId,
                Title = notification.Title ?? "",
                Message = notification.Message ?? "",
                CreatedAt = notification.CreatedAt ?? DateTime.Now,
                AnnouncementMessage = notification.NotificationType == "Announcement" && notification.Announcement != null
                ? notification.Announcement.Message ?? ""
                : notification.Message ?? "",
                AnnouncementTitle = notification.NotificationType == "Announcement" && notification.Announcement != null
                ? notification.Announcement.Title ?? ""
                : "",
                AnnouncementStartDate = notification.NotificationType == "Announcement" && notification.Announcement != null
                ? notification.Announcement.StartDate
                : null,
                AnnouncementExpiryDate = notification.NotificationType == "Announcement" && notification.Announcement != null
                ? notification.Announcement.ExpiryDate
                : null,
                AnnouncementFilePath = notification.NotificationType == "Announcement" && notification.Announcement != null
                ? notification.Announcement.FilePath ?? ""
                : "",
                AnnouncementBlocksSubmissions = notification.NotificationType == "Announcement" && notification.Announcement != null
                ? notification.Announcement.BlocksSubmissions
                : null,
                IsRead = notification.IsRead,
                NotificationType = notification.NotificationType ?? "",
                ProjectId = notification.ProjectPkId,
                ProjectName = notification.ProjectPk != null ? notification.ProjectPk.ProjectName : ""
            };

            // Mark as read
            if (notification.IsRead==false)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(model);
        }

        // POST: Notifications/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            // Soft delete
            notification.IsDeleted = true;
            notification.DeletedDate = DateTime.Now;

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(IndexStudent));
        }

        //[HttpGet]
        //public async Task<IActionResult> GetTeacherUnreadCount()
        //{
        //    var userIdStr = HttpContext.Session.GetString("UserId");
        //    var role = HttpContext.Session.GetString("UserRole");

        //    if (string.IsNullOrEmpty(userIdStr) || role != "Teacher")
        //        return Json(0);

        //    int teacherId = int.Parse(userIdStr);

        //    var count = await _context.Notifications
        //        .Where(n => n.TeacherId == teacherId && n.IsRead == false)
        //        .CountAsync();

        //    return Json(count);
        //}
        //[HttpGet]
        //public async Task<IActionResult> GetTeacherReadCount()
        //{
        //    var userIdStr = HttpContext.Session.GetString("UserId");
        //    var role = HttpContext.Session.GetString("UserRole");

        //    if (string.IsNullOrEmpty(userIdStr) || role != "Teacher")
        //        return Json(0);

        //    int teacherId = int.Parse(userIdStr);

        //    var count = await _context.Notifications
        //        .Where(n => n.TeacherId == teacherId && n.IsRead == true)
        //        .CountAsync();

        //    return Json(count);
        //}
        //[HttpPost]
        //public async Task<IActionResult> TeacherMarkAllRead()
        //{
        //    var userIdStr = HttpContext.Session.GetString("UserId");
        //    var role = HttpContext.Session.GetString("UserRole");

        //    if (string.IsNullOrEmpty(userIdStr) || role != "Teacher")
        //        return Unauthorized();

        //    int teacherId = int.Parse(userIdStr);

        //    var notifications = await _context.Notifications
        //        .Where(n => n.TeacherId == teacherId && (n.IsRead == false || n.IsRead == null))
        //        .ToListAsync();

        //    notifications.ForEach(n => n.IsRead = true);
        //    await _context.SaveChangesAsync();

        //    return Ok();
        //}

        //[HttpPost]
        //public async Task<IActionResult> TeacherMarkAsRead(int id)
        //{
        //    var notification = await _context.Notifications.FindAsync(id);

        //    if (notification == null)
        //        return NotFound();

        //    if (notification.IsRead == false)
        //    {
        //        notification.IsRead = true;
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok();
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> TeacherDelete(int id)
        //{
        //    var notification = await _context.Notifications.FindAsync(id);
        //    if (notification == null)
        //        return NotFound();

        //    _context.Notifications.Remove(notification);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(IndexTeacher));
        //}
        // ==========================
        // Teacher / Supervisor / Admin Notification Actions
        // ==========================

        [HttpGet]
        public async Task<IActionResult> GetTeacherUnreadCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr)) return Json(0);

            int teacherId = int.Parse(userIdStr);

            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null) return Json(0);

            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.NotificationType == "ProjectSubmitted");

            if (teacher.Role == "Admin")
                query = query.Where(n => n.IsReadByAdmin == false && (n.IsDeletedByAdmin == false || n.IsDeletedByAdmin == null));
            else if (teacher.Role == "Teacher") // Supervisor teacher
                query = query.Where(n => n.ProjectPk.TeacherId == teacherId && (n.IsReadByTeacher == false || n.IsReadByTeacher == null) && (n.IsDeletedByTeacher == false || n.IsDeletedByTeacher == null));

            var count = await query.CountAsync();
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacherReadCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr)) return Json(0);

            int teacherId = int.Parse(userIdStr);
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null) return Json(0);

            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.NotificationType == "ProjectSubmitted");

            if (teacher.Role == "Admin")
                query = query.Where(n => n.IsReadByAdmin == true && (n.IsDeletedByAdmin == false || n.IsDeletedByAdmin == null));
            else if (teacher.Role == "Teacher")
                query = query.Where(n => n.ProjectPk.TeacherId == teacherId && n.IsReadByTeacher == true && (n.IsDeletedByTeacher == false || n.IsDeletedByTeacher == null));

            var count = await query.CountAsync();
            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> TeacherMarkAllRead()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            int teacherId = int.Parse(userIdStr);
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null) return Unauthorized();

            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.NotificationType == "ProjectSubmitted");

            if (teacher.Role == "Admin")
                query = query.Where(n => n.IsReadByAdmin == false && (n.IsDeletedByAdmin == false || n.IsDeletedByAdmin == null));
            else if (teacher.Role == "Teacher")
                query = query.Where(n => n.ProjectPk.TeacherId == teacherId && (n.IsReadByTeacher == false || n.IsReadByTeacher == null) && (n.IsDeletedByTeacher == false || n.IsDeletedByTeacher == null));

            var notifications = await query.ToListAsync();
            notifications.ForEach(n =>
            {
                if (teacher.Role == "Admin") n.IsReadByAdmin = true;
                else if (teacher.Role == "Teacher") n.IsReadByTeacher = true;
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> TeacherMarkAsRead(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            int teacherId = int.Parse(userIdStr);
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null) return Unauthorized();

            var notification = await _context.Notifications
                .Include(n => n.ProjectPk)
                .FirstOrDefaultAsync(n => n.NotificationPkId == id);

            if (notification == null) return NotFound();

            if (teacher.Role == "Admin") notification.IsReadByAdmin = true;
            else if (teacher.Role == "Teacher")
            {
                if (notification.ProjectPk.TeacherId != teacherId) return Forbid();
                notification.IsReadByTeacher = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeacherDelete(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            int teacherId = int.Parse(userIdStr);
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null) return Unauthorized();

            var notification = await _context.Notifications
                .Include(n => n.ProjectPk)
                .FirstOrDefaultAsync(n => n.NotificationPkId == id);
            if (notification == null) return NotFound();

            if (teacher.Role == "Admin") notification.IsDeletedByAdmin = true;
            else if (teacher.Role == "Teacher")
            {
                if (notification.ProjectPk.TeacherId != teacherId) return Forbid();
                notification.IsDeletedByTeacher = true;
            }

            notification.DeletedDate = DateTime.Now;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(IndexTeacher));
        }


        [HttpPost]
        public async Task<IActionResult> AssignSchedule([FromBody] AssignDto dto)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectPkId == dto.ProjectId);

            if (project == null) return Json(new { success = false, message = "Project not found." });

            if (!DateTime.TryParse(dto.DateTime, out var parsed))
                return Json(new { success = false, message = "Invalid date/time." });

            project.ScheduleTime = parsed;
            await _context.SaveChangesAsync();

            // Notify all members
            foreach (var m in project.ProjectMembers)
            {
                var notif = new Notification
                {
                    UserId = m.StudentPkId,
                    ProjectPkId = project.ProjectPkId,
                    Title = "Schedule Assigned",
                    Message = $"Schedule: {parsed:dd MMM yyyy, hh:mm tt}",
                    NotificationType = "Schedule",
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Schedule assigned and members notified." });
        }

        [HttpPost]
        public async Task<IActionResult> AssignMeeting([FromBody] AssignDto dto)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectPkId == dto.ProjectId);

            if (project == null) return Json(new { success = false, message = "Project not found." });

            if (!DateTime.TryParse(dto.DateTime, out var parsed))
                return Json(new { success = false, message = "Invalid date/time." });

            project.MeetingTime = parsed;
            await _context.SaveChangesAsync();

            // Notify all members
            foreach (var m in project.ProjectMembers)
            {
                var notif = new Notification
                {
                    UserId = m.StudentPkId,
                    ProjectPkId = project.ProjectPkId,
                    Title = "Meeting Scheduled",
                    Message = $"Meeting: {parsed:dd MMM yyyy, hh:mm tt}",
                    NotificationType = "Meeting",
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Meeting assigned and members notified." });
        }
        public class AssignDto
        {
            public int ProjectId { get; set; }
            public string DateTime { get; set; } = string.Empty; // sent from client
        }
    }

}

