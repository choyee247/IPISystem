//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.AspNetCore.Mvc;
////using ProjectManagementSystem.Data;
//using ProjectManagementSystem.DBModels;
//using ProjectManagementSystem.Models;
//using ProjectManagementSystem.ViewModels;
//using System.Linq;

//public class WelcomeController : Controller
//{
//    private readonly PMSDbContext _context;
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly RoleManager<IdentityRole> _roleManager;

//    public WelcomeController(PMSDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
//    {
//        _context = context;
//        _userManager = userManager;
//        _roleManager = roleManager;
//    }

//    public IActionResult Index()
//    {
//        int studentCount = _context.Users.Count(u =>
//            _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == _context.Roles.FirstOrDefault(r => r.Name == "Student").Id));

//        int teacherCount = _context.Users.Count(u =>
//            _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == _context.Roles.FirstOrDefault(r => r.Name == "Teacher").Id));

//        var model = new HomeViewModel
//        {
//            ProjectCount = _context.Projects.Count(),
//            CompanyCount = _context.Companies.Count(),
//            StudentCount = studentCount,
//            TeacherCount = teacherCount
//        };

//        return View(model);
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.ViewModels;
using System.Linq;
using System.Threading.Tasks;

public class WelcomeController : Controller
{
    private readonly PMSDbContext _context;

    public WelcomeController(PMSDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Count students from Student table (replace with your actual Student table)
        int studentCount = await _context.Students.CountAsync();

        // Count teachers from Teacher table
        int teacherCount = await _context.Teachers.CountAsync();

        // Count projects and companies
        int projectCount = await _context.Projects.CountAsync();
        int companyCount = await _context.Companies.CountAsync();

        var model = new HomeViewModel
        {
            ProjectCount = projectCount,
            CompanyCount = companyCount,
            StudentCount = studentCount,
            TeacherCount = teacherCount
        };

        return View(model);
    }
}

