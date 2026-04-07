using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagementSystem.ViewComponents
{
    [ViewComponent(Name = "NotificationCount")]
    public class NotificationCountViewComponent : ViewComponent
    {
        private readonly PMSDbContext _context;

        public NotificationCountViewComponent(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var role = HttpContext.Session.GetString("UserRole")?.Trim() ?? "";
            int count = 0;

            if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
            {
                var userIdStr = HttpContext.Session.GetString("UserId");

                if (int.TryParse(userIdStr, out int teacherId))
                {
                    count = await _context.Notifications
                  .Where(n => n.NotificationType == "ProjectSubmitted"
                           && (n.IsReadByTeacher ?? false) == false
                           && (n.IsDeletedByTeacher ?? false) == false
                           && n.ProjectPk.TeacherId == teacherId)
                  .Select(n => n.ProjectPkId)   // 👈 Project only
                  .Distinct()                  // 👈 remove duplicate
                  .CountAsync();
                }
            }
            else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("here admin..................................");
                count = await _context.Notifications
                 .Where(n => n.NotificationType == "ProjectSubmitted"
                          && (n.IsReadByAdmin ?? false) == false
                          && (n.IsDeletedByAdmin ?? false) == false)
                 .Select(n => n.ProjectPkId)   // 👈 KEY FIX
                 .Distinct()                  // 👈 IMPORTANT
                 .CountAsync();

                Console.WriteLine("here admin count.................................." + count);

            }

            Console.WriteLine($"[DEBUG] Role={role}, Count={count}");

            return Content(count.ToString());
        }
    }
}