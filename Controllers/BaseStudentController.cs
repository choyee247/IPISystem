using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

public class BaseStudentController : Controller
{
    protected readonly PMSDbContext _context;

    public BaseStudentController(PMSDbContext context)
    {
        _context = context;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var studentId = HttpContext.Session.GetInt32("StudentPkId");

        if (studentId != null)
        {
            // Get the first project where student is leader
            var leaderProject = await _context.ProjectMembers
                .Where(pm => pm.StudentPkId == studentId &&
                             pm.Role.ToLower() == "leader" &&
                             pm.IsDeleted == false)
                .Select(pm => pm.ProjectPkId)
                .FirstOrDefaultAsync();

            var layoutModel = new LayoutViewModel
            {
                IsLeader = leaderProject != 0,
                LeaderProjectId = leaderProject != 0 ? (int?)leaderProject : null
            };

            ViewData["LayoutModel"] = layoutModel; 
        }

        await next();
    }
}