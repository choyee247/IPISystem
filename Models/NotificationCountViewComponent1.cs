//// File: ViewComponents/NotificationCountViewComponent.cs
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem.DBModels;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;

//namespace ProjectManagementSystem.Models
//{
//    [ViewComponent(Name = "NotificationCount1")]
//    public class NotificationCountViewComponent1 : ViewComponent
//    {
//        private readonly PMSDbContext _context;

//        public NotificationCountViewComponent1(PMSDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IViewComponentResult> InvokeAsync()
//        {
//            // Get user ID based on your authentication system
//            var userId = HttpContext.Session.GetString("UserId");//UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            Console.WriteLine("userid........................................." + userId);
//            if (string.IsNullOrEmpty(userId))
//                return Content("0");

//            try
//            {
//                // If your UserId is an int in the database:
//                if (!int.TryParse(userId, out int userIdInt))
//                    return Content("0");

//                var count = await _context.Notifications
//                    .CountAsync(n => n.UserId == userIdInt && !n.IsRead==false);

//                return Content(count.ToString());
//            }
//            catch (Exception ex)
//            {
//                // Log error if needed
//                return Content("0");
//            }
//        }
//    }
//}