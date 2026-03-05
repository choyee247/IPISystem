using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagementSystem.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly PMSDbContext _context;

        public NotificationViewComponent(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return View(new List<NotificationViewModel>());

            int teacherId = int.Parse(userIdStr);

            // Fetch notifications for teacher's projects
            var notificationsQuery = await _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.NotificationType == "ProjectSubmitted"
                            && (n.IsReadByTeacher == false || n.IsReadByTeacher == null)
                            && n.ProjectPk.TeacherId == teacherId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(); // bring to memory

            // Group by ProjectId to avoid duplicates per project
            var notifications = notificationsQuery
                .GroupBy(n => n.ProjectPkId)
                .Select(g => g.OrderByDescending(n => n.CreatedAt).First())
                .Take(4)
                .Select(n => new NotificationViewModel
                {
                    Id = n.NotificationPkId,
                    Message = n.Message ?? "",
                    CreatedAt = n.CreatedAt ?? System.DateTime.Now,
                    ProjectId = n.ProjectPkId ?? 0,
                    ProjectName = n.ProjectPk != null ? n.ProjectPk.ProjectName : "No Project",
                    IsReadByTeacher = n.IsReadByTeacher ?? false,
                    IsDeletedByTeacher = n.IsDeletedByTeacher ?? false
                })
                .ToList();

            return View(notifications);
        }
    }
}
