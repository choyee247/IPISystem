using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;

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
            var isLeader = await _context.ProjectMembers
                .AnyAsync(pm =>
                    pm.StudentPkId == studentId &&
                    pm.Role.ToLower() == "leader" &&
                    pm.IsDeleted == false);

            ViewBag.IsLeader = isLeader;
        }

        await next(); // continue pipeline
    }
}