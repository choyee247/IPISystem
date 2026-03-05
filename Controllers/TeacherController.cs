// Controllers/TeacherController.cs
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using System.Diagnostics;

namespace ProjectManagementSystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly PMSDbContext _context;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(PMSDbContext context, ILogger<TeacherController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ---------------- SESSION CHECK ----------------
        private bool IsLoggedIn() => HttpContext.Session.GetString("UserId") != null;
        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";
        private bool IsLoggedInTeacher()
        {
            return HttpContext.Session.GetString("UserRole") == "Teacher"
                && HttpContext.Session.GetString("UserId") != null;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsLoggedInTeacher())
                return RedirectToAction("Login", "Teacher");

            int teacherId = int.Parse(HttpContext.Session.GetString("UserId")!);

            // Assigned Students Count
            var assignedStudentsCount = await _context.TeacherStudents
                .CountAsync(ts => ts.TeacherId == teacherId && ts.IsActive);

            // Projects Under Supervision Count
            var projectsCount = await _context.Projects
                .CountAsync(p => p.TeacherId == teacherId && (p.IsDeleted == null || p.IsDeleted == false));

            // Companies Assigned to Teacher Count
            var companiesCount = await _context.TeacherCompanies
                .CountAsync(tc => tc.TeacherId == teacherId);

            ViewBag.FullName = HttpContext.Session.GetString("UserName") ?? "Teacher";
            ViewBag.AssignedStudentsCount = assignedStudentsCount;
            ViewBag.ProjectsCount = projectsCount;
            ViewBag.CompaniesCount = companiesCount;

            return View();
        }

        // ---------------------- INDEX ----------------------
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Teacher");

            int pageSize = 3; // teachers per page

            // Include Department & AcademicYear, and get assigned students count
            var query = _context.Teachers
                .Include(t => t.DepartmentPk)
                .Include(t => t.AcademicYearPk)
                .Select(t => new
                {
                    Teacher = t,
                    AssignedStudentsCount = _context.TeacherStudents
                                                .Where(ts => ts.TeacherId == t.Id && ts.IsActive)
                                                .Count()
                })
                .OrderBy(t => t.Teacher.FullName);

            int totalTeachers = await query.CountAsync();

            var teachersWithCount = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalTeachers / (double)pageSize);

            return View(teachersWithCount.Select(x => new
            {
                x.Teacher,
                x.AssignedStudentsCount
            }));
        }

        // ---------------------- CREATE ----------------------
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Departments = _context.StudentDepartments.ToList();
            ViewBag.AcademicYears = _context.AcademicYears.ToList();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Teacher model)
        {
            if (!IsLoggedIn() || !IsAdmin())
                return RedirectToAction("Login", "Admin");

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                model.CreatedDate = DateTime.Now;
                _context.Teachers.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Teacher created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher");
                TempData["ErrorMessage"] = "An error occurred while creating the teacher.";
                PopulateDropdowns(model);
                return View(model);
            }
        }

        // ---------------------- EDIT ----------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            // ✅ Assign Departments
            ViewBag.Departments = await _context.StudentDepartments.ToListAsync();

            // ✅ Assign AcademicYears
            ViewBag.AcademicYears = await _context.AcademicYears.ToListAsync();

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Teacher model)
        {
            if (!IsLoggedIn() || !IsAdmin())
                return RedirectToAction("Login", "Admin");

            if (ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var teacher = await _context.Teachers.FindAsync(model.Id);
            if (teacher == null) return NotFound();

            teacher.FullName = model.FullName;
            teacher.Email = model.Email;
            teacher.Role = model.Role;
            teacher.DepartmentPkId = model.DepartmentPkId;
            teacher.AcademicYearPkId = model.AcademicYearPkId;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Teacher updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Teacher/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Teacher");

            var teacher = await _context.Teachers
                .Include(t => t.DepartmentPk)
                .Include(t => t.AcademicYearPk)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            var assignedStudents = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == id && ts.IsActive)
                .Include(ts => ts.StudentPk)
                .Select(ts => ts.StudentPk)
                .ToListAsync();

            var studentProjectRoles = await _context.ProjectMembers
                .Include(pm => pm.StudentPk)
                .Include(pm => pm.ProjectPk)
                .Where(pm =>
                    pm.ProjectPk.TeacherId == id &&
                    pm.IsDeleted == false)
                .Select(pm => new StudentProjectRoleViewModel
                {
                    StudentPkId = pm.StudentPkId,
                    StudentName = pm.StudentPk.StudentName,
                    ProjectPkId = pm.ProjectPkId,
                    ProjectName = pm.ProjectPk.ProjectName,
                    Role = pm.Role
                })
                .OrderBy(x => x.StudentName)
                .ToListAsync();

            var assignedCompanies = await _context.TeacherCompanies
                .Where(tc => tc.TeacherId == id && tc.IsActive)
                .Include(tc => tc.CompanyPk)
                .Select(tc => tc.CompanyPk)
                .ToListAsync();

            var studentCompanyAssignments = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == id && ts.IsActive)
                .Select(ts => new StudentCompanyViewModel
                {
                    StudentId = ts.StudentPk.StudentPkId,
                    StudentName = ts.StudentPk.StudentName,
                    CompanyName = _context.StudentCompanies
                        .Where(sc => sc.StudentId == ts.StudentPk.StudentPkId && sc.IsActive)
                        .Select(sc => sc.Company.CompanyName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var viewModel = new TeacherDetailViewModel
            {
                TeacherId = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Role = teacher.Role,
                DepartmentName = teacher.DepartmentPk?.DepartmentName,
                AcademicYear = teacher.AcademicYearPk?.YearRange,
                CreatedDate = teacher.CreatedDate,
                StudentCompanyAssignments = studentCompanyAssignments,
                AssignedStudents = assignedStudents,
                StudentProjectRoles = studentProjectRoles,
                Companies = assignedCompanies         
            };

            return View(viewModel);
        }

        public async Task<IActionResult> AssignCompany(int studentId, int teacherId)
        {
            var companies = await _context.TeacherCompanies
                .Where(tc => tc.TeacherId == teacherId && tc.IsActive)
                .Select(tc => tc.CompanyPk)
                .ToListAsync();

            var viewModel = new AssignCompaniesViewModel
            {
                StudentId = studentId,
                TeacherId = teacherId,
                Companies = companies.Select(c => new SelectListItem
                {
                    Value = c.CompanyPkId.ToString(),
                    Text = c.CompanyName
                }).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> AssignCompany(AssignCompaniesViewModel model)
        {
            var existing = await _context.StudentCompanies
                .FirstOrDefaultAsync(sc => sc.StudentId == model.StudentId && sc.IsActive);

            if (existing != null)
            {
                existing.CompanyId = model.CompanyId;
            }
            else
            {
                _context.StudentCompanies.Add(new StudentCompany
                {
                    StudentId = model.StudentId,
                    CompanyId = model.CompanyId,
                    TeacherId = model.TeacherId,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = model.TeacherId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Teacher");

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            var projects = await _context.Projects
                                         .Where(p => p.TeacherId == teacher.Id)
                                         .ToListAsync();

            foreach (var project in projects)
            {
                project.TeacherId = null;
            }

            var notifications = await _context.Notifications
                                    .Where(n => n.TeacherId == teacher.Id)
                                    .ToListAsync();
            foreach (var notification in notifications)
            {
                notification.TeacherId = null;
            }
            _context.Teachers.Remove(teacher);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Teacher deleted successfully! Projects remain intact.";
            return RedirectToAction(nameof(Index));
        }


        // ---------------------- ASSIGN STUDENTS ----------------------
        // TeacherController.cs
        [HttpGet]
        public async Task<IActionResult> AssignStudents(int teacherId)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Students)
                .FirstOrDefaultAsync(t => t.Id == teacherId);

            if (teacher == null)
                return NotFound();

            // Get all students (filter by department, academic year etc.)
            var allStudents = await _context.Students
                .Where(s => s.DepartmentPkId == teacher.DepartmentPkId)
                .ToListAsync();

            var viewModel = new AssignStudentsViewModel
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.FullName,
                Students = allStudents.Select(s => new SelectListItem
                {
                    Value = s.StudentPkId.ToString(),
                    Text = s.StudentName,
                    Selected = teacher.Students.Any(ts => ts.StudentPkId == s.StudentPkId)
                }).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStudents(AssignStudentsViewModel model)
        {
            if (ModelState.IsValid)
                return View(model);

            // Remove old assignments for this teacher
            var oldAssignments = _context.TeacherStudents
                .Where(ts => ts.TeacherId == model.TeacherId);
            _context.TeacherStudents.RemoveRange(oldAssignments);

            // Add new assignments
            foreach (var studentId in model.SelectedStudentIds)
            {
                _context.TeacherStudents.Add(new TeacherStudent
                {
                    TeacherId = model.TeacherId,
                    StudentPkId = studentId,
                    AcademicYearPkId = 1, // TODO: set the correct AcademicYear
                    AssignedDate = DateTime.Now,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Students assigned successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AssignCompanies(int teacherId)
        {
            if (!IsLoggedIn() || !IsAdmin())
                return RedirectToAction("Login", "Admin");

            var teacher = await _context.Teachers
                .Include(t => t.TeacherCompanies)
                .FirstOrDefaultAsync(t => t.Id == teacherId);

            if (teacher == null) return NotFound();

            var companies = await _context.Companies.ToListAsync();

            var viewModel = new AssignCompaniesViewModel
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.FullName,
                Companies = companies.Select(c => new SelectListItem
                {
                    Value = c.CompanyPkId.ToString(),
                    Text = c.CompanyName,
                    Selected = teacher.TeacherCompanies
                        .Any(tc => tc.CompanyPkId == c.CompanyPkId && tc.IsActive)
                }).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCompanies(AssignCompaniesViewModel model)
        {
            if (!IsLoggedIn() || !IsAdmin())
                return RedirectToAction("Login", "Admin");

            // Remove old
            var old = _context.TeacherCompanies
                .Where(tc => tc.TeacherId == model.TeacherId);
            _context.TeacherCompanies.RemoveRange(old);

            // Add new
            foreach (var companyId in model.SelectedCompanyIds)
            {
                _context.TeacherCompanies.Add(new TeacherCompany
                {
                    TeacherId = model.TeacherId,
                    CompanyPkId = companyId,
                    AssignedDate = DateTime.Now,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Companies assigned successfully!";
            return RedirectToAction(nameof(Index));
        }


        // ---------------------- HELPERS ----------------------
        private void PopulateDropdowns(Teacher? model = null)
        {
            ViewBag.Departments = new SelectList(_context.StudentDepartments, "DepartmentPkId", "DepartmentName", model?.DepartmentPkId);
            ViewBag.AcademicYears = new SelectList(_context.AcademicYears.Where(y => y.IsActive == true), "AcademicYearPkId", "YearRange", model?.AcademicYearPkId);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature?.Error, "Error occurred in TeacherController");

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
