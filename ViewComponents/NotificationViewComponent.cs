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
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdStr))
                return View(new List<NotificationViewModel>());

            int userId = int.Parse(userIdStr);

            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.ProjectPk)
                .Where(n => n.NotificationType == "ProjectSubmitted");

            if (role == "Teacher")
            {
                query = query.Where(n => n.ProjectPk.TeacherId == userId);
            }

            // Filter out deleted notifications per role
            var notifications = await query
                .GroupBy(n => n.ProjectPkId)
                .Select(g => g.OrderByDescending(n => n.CreatedAt).FirstOrDefault())
                .ToListAsync();

            var viewModel = notifications
                .Select(n => new NotificationViewModel
                {
                    Id = n.NotificationPkId,
                    Message = n.Message ?? "",
                    CreatedAt = n.CreatedAt ?? DateTime.Now,
                    ProjectId = n.ProjectPkId ?? 0,
                    ProjectName = n.ProjectPk?.ProjectName ?? "No Project",
                    IsReadByTeacher = n.IsReadByTeacher ?? false,
                    IsDeletedByTeacher = n.IsDeletedByTeacher ?? false,
                    IsReadByAdmin = n.IsReadByAdmin ?? false,
                    IsDeletedByAdmin = n.IsDeletedByAdmin ?? false
                })
                .Where(n => role == "Teacher" ? !n.IsDeletedByTeacher : !n.IsDeletedByAdmin)
                .OrderByDescending(n => n.CreatedAt)
                .Take(4) // Limit for notification panel
                .ToList();

            return View(viewModel);
        }
    }
}