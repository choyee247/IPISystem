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

        // ---------------------
        // Student Notifications
        // ---------------------
        public async Task<IActionResult> IndexStudent()
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return RedirectToAction("Login", "StudentLogin");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);

            if (student == null)
                return NotFound();

            var notifications = await _context.Notifications
                .Include(n => n.Announcement)
                .Where(n => n.UserId == student.StudentPkId)
                .Where(n => n.IsDeleted == false || n.IsDeleted == null)
                .Where(n => n.NotificationType != "ProjectSubmitted" && n.NotificationType != "ProjectStatus")

                .Where(n => n.NotificationType != "Announcement" ||
                    (n.NotificationType == "Announcement" && n.Announcement != null && n.Announcement.IsActive==true))

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
                    DeadlineStatus = "" // Optional: Add deadline calculation if relevant
                })
                .ToListAsync();

            return View(notifications);
        }

        //public async Task<IActionResult> IndexStudent()
        //{
        //    var userId = HttpContext.Session.GetInt32("StudentPkId"); // Student session
        //    if (userId == null)
        //        return RedirectToAction("Login", "StudentLogin");

        //    var notifications = await _context.Notifications
        //      .Include(n => n.ProjectPk)
        //      .Where(n => n.UserId == userId
        //                  && n.IsDeleted == false
        //                  && (n.NotificationType == "Announcement" ||
        //                      n.NotificationType == "Response" ||
        //                      n.NotificationType == "Schedule" ||
        //                      n.NotificationType == "Meeting" ||
        //                      n.NotificationType == "ProjectStatus"))
        //      .OrderByDescending(n => n.CreatedAt)
        //      .Select(n => new NotificationViewModel
        //      {
        //          Id = n.NotificationPkId,
        //          Title = n.Title ?? "",
        //          Message = n.Message ?? "",
        //          IsRead = n.IsRead ?? false,
        //          CreatedAt = n.CreatedAt ?? DateTime.Now,
        //          ProjectId = n.ProjectPkId,
        //          ProjectName = n.ProjectPk.ProjectName ?? "",
        //          NotificationType = n.NotificationType ?? "",
        //          //DeadlineStatus = (n.NotificationType == "Meeting" && n.ProjectPk.MeetingTime != null)
        //          //    ? GetDeadlineStatus(n.ProjectPk.MeetingTime.Value)
        //          //    : ""
        //      })
        //      .ToListAsync();


        //    return View("IndexStudent", notifications);

        //    //var userId = HttpContext.Session.GetInt32("StudentPkId");
        //    //if (userId == null)
        //    //    return RedirectToAction("Login", "StudentLogin");

        //    //var notifications = await _context.Notifications
        //    //    .Include(n => n.ProjectPk)
        //    //    .Where(n => n.UserId == userId &&
        //    //                n.IsDeleted == false &&
        //    //                (n.NotificationType == "Announcement" ||
        //    //                 n.NotificationType == "Response" ||
        //    //                 n.NotificationType == "Schedule" ||
        //    //                 n.NotificationType == "Meeting"||
        //    //                 n.NotificationType == "ProjectStatus"))

        //    //    .OrderByDescending(n => n.CreatedAt)
        //    //    .Select(n => new NotificationViewModel
        //    //    {
        //    //        Id = n.NotificationPkId,
        //    //        Title = n.Title ?? "",
        //    //        Message = n.Message ?? "",
        //    //        IsRead = n.IsRead ?? false,
        //    //        CreatedAt = n.CreatedAt ?? DateTime.Now,
        //    //        ProjectId = n.ProjectPkId,
        //    //        ProjectName = n.ProjectPk.ProjectName ?? "",
        //    //        NotificationType = n.NotificationType ?? "",
        //    //        //DeadlineStatus = (n.NotificationType == "Meeting" && n.ProjectPk.MeetingTime != null)
        //    //        //                 ? GetDeadlineStatus(n.ProjectPk.MeetingTime.Value)
        //    //        //                 : ""
        //    //    })
        //    //    .ToListAsync();

        //    //return View("IndexStudent", notifications);
        //}

        //Helper method to calculate deadline status

        //private string GetDeadlineStatus(DateTime meetingTime)
        //{
        //    var hoursLeft = (meetingTime - DateTime.Now).TotalHours;
        //    if (hoursLeft <= 1 && hoursLeft > 0)
        //        return "Starting Soon";
        //    else if (hoursLeft <= 24 && hoursLeft > 1)
        //        return "Tomorrow";
        //    else if (hoursLeft <= 0)
        //        return "Missed";
        //    else
        //        return "";
        //}

        ///**********????/////
        //public IActionResult IndexTeacher()
        //{
        //    var userIdStr = HttpContext.Session.GetString("UserId");
        //    var userRole = HttpContext.Session.GetString("UserRole");

        //    if (string.IsNullOrEmpty(userIdStr) || userRole != "Teacher")
        //        return RedirectToAction("Login", "AdminLogin");

        //    // Load ProjectSubmitted notifications only
        //    var notifications = _context.Notifications
        //        .Include(n => n.ProjectPk)
        //        .Where(n => n.NotificationType == "ProjectSubmitted")
        //        .Where(n => n.IsRead == false)
        //        .OrderByDescending(n => n.CreatedAt)
        //        .Take(5)
        //        .Select(n => new NotificationViewModel
        //        {
        //            Id = n.NotificationPkId,
        //            Message = n.Message,
        //            CreatedAt = (DateTime)n.CreatedAt,
        //            ProjectId = n.ProjectPkId ?? 0,
        //            ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "No Project",
        //            IsRead = n.IsRead,
        //        })
        //        .ToList();

        //    // Send to View
        //    ViewBag.ProjectSubmittedNotifications = notifications;

        //    return View();
        //}

        public IActionResult IndexTeacher()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || userRole != "Teacher")
                return RedirectToAction("Login", "AdminLogin");

            int teacherId = int.Parse(userIdStr);
            Console.WriteLine("teacherid.............." +teacherId);
            // Load ProjectSubmitted notifications
            var notifications = _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.TeacherId == teacherId && n.UserId == teacherId)
                //.Where(n => n.TeacherId == teacherId)
                .Where(n => n.NotificationType == "ProjectSubmitted")
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationViewModel
                {
                    Id = n.NotificationPkId,
                    Message = n.Message,
                    CreatedAt = (DateTime)n.CreatedAt,
                    ProjectId = n.ProjectPkId ?? 0,
                    ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "No Project",
                    IsDeleted = n.IsDeleted ?? false,
                    IsRead = n.IsRead ?? false,
                })
                .ToList();

            ViewBag.Notifications = notifications;
            ViewBag.UnreadCount = notifications.Count(n => n.IsRead==false);
            ViewBag.ReadCount = notifications.Count(n => n.IsRead==true);
            Console.WriteLine("teacher noti................" + System.Text.Json.JsonSerializer.Serialize(notifications));
            return View(notifications);
        }




        // ---------------------
        // Shared Actions
        // ---------------------
        // Mark a single notification as read
        //[HttpPost]
        //public async Task<IActionResult> MarkAsRead(int id)
        //{
        //    var notification = await _context.Notifications.FindAsync(id);
        //    if (notification != null)
        //    {
        //        notification.IsRead = true;
        //        await _context.SaveChangesAsync();
        //    }
        //    return Ok();
        //}

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

        //// Mark all notifications as read
        //[HttpPost]
        //public async Task<IActionResult> MarkAllAsRead()
        //{
        //    var userId = HttpContext.Session.GetInt32("StudentPkId"); // use correct session key
        //    if (userId == null)
        //        return Json(new { success = false, count = 0 });

        //    var notifications = await _context.Notifications
        //        .Where(n => n.UserId == userId && !(n.IsRead ?? false) && !(n.IsDeleted ?? false))
        //        .ToListAsync();

        //    foreach (var notif in notifications)
        //        notif.IsRead = true;

        //    await _context.SaveChangesAsync();
        //    return Json(new { success = true, count = notifications.Count });
        //}
        //[HttpPost]
        //public async Task<IActionResult> MarkAllRead()
        //{
        //    var rollNumber = HttpContext.Session.GetString("RollNumber");
        //    if (string.IsNullOrEmpty(rollNumber))
        //        return Unauthorized();

        //    var student = await _context.Students.FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);
        //    if (student == null)
        //        return NotFound();

        //    var notifications = await _context.Notifications
        //        .Where(n => n.UserId == student.StudentPkId && (n.IsRead == false || n.IsRead == null))
        //        .ToListAsync();

        //    notifications.ForEach(n => n.IsRead = true);
        //    await _context.SaveChangesAsync();

        //    return Ok();
        //}
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

        // Get count of unread notifications
        //public async Task<IActionResult> GetUnreadCount()
        //{
        //    var userId = HttpContext.Session.GetInt32("StudentPkId");
        //    if (userId == null) return Json(0);

        //    var count = await _context.Notifications
        //        .Where(n => n.UserId == userId && !(n.IsRead ?? false) && !(n.IsDeleted ?? false))
        //        .CountAsync();

        //    return Json(count);
        //}

        //// Get count of read notifications
        //public async Task<IActionResult> GetReadCount()
        //{
        //    var userId = HttpContext.Session.GetInt32("StudentPkId");
        //    if (userId == null) return Json(0);

        //    var count = await _context.Notifications
        //        .Where(n => n.UserId == userId && (n.IsRead ?? false) && !(n.IsDeleted ?? false))
        //        .CountAsync();

        //    return Json(count);
        //}


        // GET: Notifications/Details/5
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



        // -----------------------------
        // Delete Project
        // -----------------------------
        //[HttpPost, ActionName("Delete")]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var project = await _context.Projects
        //        .Include(p => p.Notifications)
        //        .Include(p => p.ProjectMembers)
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == id);

        //    if (project == null)
        //        return NotFound();

        //    // 1️⃣ Delete related notifications first (manual cascade)
        //    if (project.Notifications.Any())
        //    {
        //        _context.Notifications.RemoveRange(project.Notifications);
        //    }

        //    // 2️⃣ Optionally, delete related project members if needed
        //    if (project.ProjectMembers.Any())
        //    {
        //        _context.ProjectMembers.RemoveRange(project.ProjectMembers);
        //    }

        //    // 3️⃣ Delete the project itself
        //    _context.Projects.Remove(project);

        //    await _context.SaveChangesAsync();

        //    TempData["Success"] = "Project and related data deleted successfully.";
        //    return RedirectToAction("Index"); // Adjust redirect as needed
        //}

        [HttpGet]
        public async Task<IActionResult> GetTeacherUnreadCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || role != "Teacher")
                return Json(0);

            int teacherId = int.Parse(userIdStr);

            var count = await _context.Notifications
                .Where(n => n.TeacherId == teacherId && n.IsRead == false)
                .CountAsync();

            return Json(count);
        }
        [HttpGet]
        public async Task<IActionResult> GetTeacherReadCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || role != "Teacher")
                return Json(0);

            int teacherId = int.Parse(userIdStr);

            var count = await _context.Notifications
                .Where(n => n.TeacherId == teacherId && n.IsRead == true)
                .CountAsync();

            return Json(count);
        }
        [HttpPost]
        public async Task<IActionResult> TeacherMarkAllRead()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr) || role != "Teacher")
                return Unauthorized();

            int teacherId = int.Parse(userIdStr);

            var notifications = await _context.Notifications
                .Where(n => n.TeacherId == teacherId && (n.IsRead == false || n.IsRead == null))
                .ToListAsync();

            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> TeacherMarkAsRead(int id)
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeacherDelete(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);
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

        // -----------------------------
        // Assign Meeting
        // -----------------------------
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

        // -----------------------------
        // DTO Class
        // -----------------------------
        public class AssignDto
        {
            public int ProjectId { get; set; }
            public string DateTime { get; set; } = string.Empty; // sent from client
        }
    }

}

