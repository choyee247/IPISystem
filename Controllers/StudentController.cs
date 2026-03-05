using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace ProjectManagementSystem.Controllers
{
    public class StudentController : BaseStudentController
    {
        private readonly PMSDbContext _context;
        private readonly IWebHostEnvironment _env;
        public StudentController(PMSDbContext context, IWebHostEnvironment env)
             : base(context)
        {
            _context = context;
            _env = env;

        }
        public async Task<IActionResult> Index(
      int? yearId,
      string searchString = "",
      int page = 1,
      int pageSize = 20)
        {
            // 1️⃣ Logged-in user
            var userRole = HttpContext.Session.GetString("UserRole");
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdString))
                userId = int.Parse(userIdString);

            // 2️⃣ Academic Years list
            var years = await _context.AcademicYears
                .OrderByDescending(y => y.YearRange)
                .ToListAsync();
            ViewBag.AcademicYears = years;
            ViewBag.SelectedYear = yearId;

            // 3️⃣ Students query
            var studentsQuery = _context.Students
                .Include(s => s.EmailPk)
                .Include(s => s.DepartmentPk)
                .Include(s => s.AcademicYearPk)
                .Where(s => s.IsDeleted == false)
                .AsQueryable();

            // 4️⃣ Filter by year
            if (yearId.HasValue)
                studentsQuery = studentsQuery.Where(s => s.AcademicYearPkId == yearId.Value);

            // 5️⃣ Filter by teacher role
            if (userRole == "Teacher")
            {
                studentsQuery = studentsQuery
                    .Where(s => _context.TeacherStudents
                        .Any(ts => ts.StudentPkId == s.StudentPkId
                                   && ts.TeacherId == userId
                                   && ts.IsActive));
            }
            else if (userRole != "Admin")
            {
                return RedirectToAction("Login", "Teacher");
            }

            // 6️⃣ Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                studentsQuery = studentsQuery
                    .Where(s => s.StudentName.Contains(searchString)
                             || s.EmailPk.RollNumber.Contains(searchString)
                             || s.EmailPk.EmailAddress.Contains(searchString));
            }

            studentsQuery = studentsQuery.OrderBy(s => s.StudentName);

            // 7️⃣ Project info
            var studentsWithProjects = await studentsQuery
                .Select(s => new StudentWithProjectViewModel
                {
                    StudentPkId = s.StudentPkId,
                    StudentName = s.StudentName,
                    RollNumber = s.EmailPk.RollNumber,
                    Email = s.EmailPk.EmailAddress,
                    DepartmentName = s.DepartmentPk.DepartmentName,
                    AcademicYear = s.AcademicYearPk.YearRange,
                    HasProject =
                    _context.Projects.Any(p =>
                        (p.StudentPkId == s.StudentPkId) &&
                        (p.IsDeleted == null || p.IsDeleted == false)
                    )
                    ||
                    _context.ProjectMembers.Any(pm =>
                        pm.StudentPkId == s.StudentPkId &&
                        pm.IsDeleted == false
                    )
                                })
                .ToPagedListAsync(page, pageSize);

            ViewBag.SearchString = searchString;

            return View(studentsWithProjects);
        }



        public async Task<IActionResult> Detail(int id)
        {
            var student = await _context.Students
                .Include(s => s.EmailPk)
                .Include(s => s.DepartmentPk)
                .Include(s => s.NrcPk)
                .Include(s => s.NrctypePk)
                .Include(s => s.AcademicYearPk)
                .Include(s => s.ProjectMembers)
                    .ThenInclude(pm => pm.ProjectPk)
                        .ThenInclude(p => p.LanguagePk)
                .Include(s => s.ProjectMembers)
                    .ThenInclude(pm => pm.ProjectPk)
                        .ThenInclude(p => p.FrameworkPk)
                .Include(s => s.ProjectMembers)
                    .ThenInclude(pm => pm.ProjectPk)
                        .ThenInclude(p => p.CompanyPk)
                            .ThenInclude(c => c.CityPk)
                .FirstOrDefaultAsync(s => s.StudentPkId == id && s.IsDeleted == false);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }
        public IActionResult Create()
        {
            var nrcTypes = _context.Nrctypes.ToList();
            var townships = _context.Nrctownships.ToList();
            var regionCodes = townships.Select(t => t.RegionCodeM).Distinct().ToList();
            var departments = _context.StudentDepartments.OrderBy(d => d.DepartmentName).ToList();
            var years = _context.AcademicYears.OrderByDescending(y => y.YearRange).ToList();

            var viewModel = new NRCFormViewModel
            {
                Student = new DBModels.Student
                {
                    EmailPk = new DBModels.Email()
                },
                NRCTypeList = nrcTypes,
                RegionCodeMList = regionCodes,
                TownshipList = townships,
                DepartmentList = departments,
                AcademicYearList = years,
            };
            var academicYearId = HttpContext.Session.GetInt32("AcademicYearPkId");

            if (academicYearId.HasValue)
            {
                viewModel.Student.AcademicYearPkId = academicYearId.Value;
            }

            var roll = HttpContext.Session.GetString("RollNumber");
            var email = HttpContext.Session.GetString("EmailAddress");
            var role = HttpContext.Session.GetString("UserRole");

            HttpContext.Session.SetInt32("CurrentStep", 2);

            if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(roll))
            {
                var emailEntry = _context.Emails
                    .FirstOrDefault(e => e.RollNumber == roll && (e.IsDeleted ?? false) == false);

                if (emailEntry != null)
                {
                    email = emailEntry.EmailAddress;
                    HttpContext.Session.SetString("EmailAddress", email);
                }
            }

            if (!string.IsNullOrEmpty(role) && role == "Leader")
            {
                viewModel.Student.AcademicYearPkId = _context.Emails
                    .Where(e => e.RollNumber == roll && e.EmailAddress == email && e.IsDeleted == false)
                    .Select(e => e.AcademicYearPkId)
                    .FirstOrDefault();
            }

            // Auto fill
            viewModel.Student.EmailPk.RollNumber = roll;
            viewModel.Student.EmailPk.EmailAddress = email;

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NRCFormViewModel model, IFormFile? ProfilePhoto, string? nextAction)
        {
            // Always fill dropdown lists
            model.NRCTypeList = _context.Nrctypes.ToList();
            model.RegionCodeMList = _context.Nrctownships.Select(t => t.RegionCodeM).Distinct().ToList();
            model.TownshipList = _context.Nrctownships.ToList();
            model.DepartmentList = _context.StudentDepartments.OrderBy(d => d.DepartmentName).ToList();
            model.AcademicYearList = _context.AcademicYears.OrderByDescending(y => y.YearRange).ToList();

            // Get session values
            var roll = HttpContext.Session.GetString("RollNumber");
            var email = HttpContext.Session.GetString("EmailAddress");

            if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(roll))
            {
                var emailDb = _context.Emails.FirstOrDefault(e => e.RollNumber == roll && (e.IsDeleted ?? false) == false);
                if (emailDb != null)
                {
                    email = emailDb.EmailAddress;
                    HttpContext.Session.SetString("EmailAddress", email);
                }
            }

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email record not found. Please log in again.";
                return RedirectToAction("Login", "StudentLogin");
            }

            var emailEntry = _context.Emails.FirstOrDefault(e => e.RollNumber == roll && e.EmailAddress == email && e.IsDeleted == false);

            if (emailEntry != null)
            {
                model.Student.AcademicYearPkId = emailEntry.AcademicYearPkId;
                model.Student.EmailPkId = emailEntry.EmailPkId;
            }

            // Auto fill for view model
            if (model.Student.EmailPk == null)
                model.Student.EmailPk = new Email();

            var sessionYearId = HttpContext.Session.GetInt32("AcademicYearPkId");

            if (sessionYearId.HasValue)
            {
                model.Student.AcademicYearPkId = sessionYearId.Value;
            }

            model.Student.EmailPk.RollNumber = roll;
            model.Student.EmailPk.EmailAddress = email;

            if (ModelState.IsValid)
            {
                return View(model);
            }

            // Profile photo upload
            if (ProfilePhoto != null && ProfilePhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfilePhoto.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePhoto.CopyToAsync(fileStream);
                }

                model.Student.ProfilePhotoUrl = "/uploads/students/" + uniqueFileName;
            }
            else
            {
                model.Student.ProfilePhotoUrl = "/uploads/students/default-avatar.png";
            }

            model.Student.CreatedDate = DateTime.Now;
            model.Student.IsDeleted = false;

            _context.Students.Add(model.Student);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("StudentPkId", model.Student.StudentPkId);

            if (!string.IsNullOrEmpty(nextAction) && nextAction == "CreateProject")
            {
                return RedirectToAction("Create", "Project");
            }
            HttpContext.Session.SetInt32("CurrentStep", 3);

            return RedirectToAction("Dashboard", "Student");
        }


        // JSON: Get townships by RegionCode
        [HttpGet]
        public JsonResult GetTownshipsByRegion(string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode))
            {
                return Json(new List<object>());
            }

            var townships = _context.Nrctownships
                .Where(t => t.RegionCodeM == regionCode)
                .Select(t => new
                {
                    nRC_pkId = t.NrcPkId,
                    townshipCode_M = t.TownshipCodeM,
                    townshipCode_E = t.TownshipCodeE,
                    townshipName = t.TownshipName
                })
                .OrderBy(t => t.townshipCode_M)
                .ToList();

            return Json(townships);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students
                .Include(s => s.EmailPk)
                .Include(s => s.NrcPk)
                .Include(s => s.NrctypePk)
                .Include(s => s.DepartmentPk)
                .Include(s=>s.AcademicYearPk)
                
                .FirstOrDefaultAsync(s => s.StudentPkId == id);

            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new NRCFormViewModel
            {
                Student = student,
                NRCTypeList = _context.Nrctypes.ToList(),
                //RegionCodeMList = _context.Nrctownships.Select(t => t.RegionCodeM).Distinct().ToList(),
                RegionCodeMList = _context.Nrctownships
                .Select(t => t.RegionCodeM.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
                TownshipList = _context.Nrctownships.ToList(),
                DepartmentList = _context.StudentDepartments.OrderBy(d => d.DepartmentName).ToList(),
                AcademicYearList = _context.AcademicYears.OrderByDescending(a => a.YearRange).ToList(),
                ProjectMembers = _context.ProjectMembers.Where(pm => pm.IsDeleted == false).ToList()
            };

            return View(viewModel);
        }
  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NRCFormViewModel model)
        {
            if (id != model.Student.StudentPkId)
                return NotFound();

            // Reload dropdown lists regardless of ModelState
            model.NRCTypeList = _context.Nrctypes.ToList();
            //model.RegionCodeMList = _context.Nrctownships.Select(t => t.RegionCodeM).Distinct().ToList();
            model.RegionCodeMList = _context.Nrctownships
            .Select(t => t.RegionCodeM.Trim())
            .Distinct()
            .OrderBy(x => x)
            .ToList();
            model.TownshipList = _context.Nrctownships.ToList();
            model.DepartmentList = _context.StudentDepartments.OrderBy(d => d.DepartmentName).ToList();
            model.AcademicYearList = _context.AcademicYears.OrderByDescending(a => a.YearRange).ToList();

            if (ModelState.IsValid)
            {
                // Return view with validation errors and reloaded dropdowns
                return View(model);
            }

            try
            {
                var studentInDb = await _context.Students.FindAsync(id);
                if (studentInDb == null)
                    return NotFound();

                // Update student fields
                //studentInDb.StudentName = model.Student.StudentName;
                //studentInDb.RollNumber = model.Student.RollNumber;
                studentInDb.EmailPk = model.Student.EmailPk;
                studentInDb.PhoneNumber = model.Student.PhoneNumber;
                studentInDb.DepartmentPkId = model.Student.DepartmentPkId;
                studentInDb.AcademicYearPkId = model.Student.AcademicYearPkId;
                studentInDb.NrctypePkId = model.Student.NrctypePkId;
                studentInDb.NrcPkId = model.Student.NrcPkId;
                studentInDb.Nrcnumber = model.Student.Nrcnumber;
                studentInDb.CreatedBy = model.Student.CreatedBy;

                // ---------------------------
                // Profile Photo Upload
                // ---------------------------
                if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "students");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ProfilePhoto.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePhoto.CopyToAsync(stream);
                    }

                    // Save relative path to DB
                    studentInDb.ProfilePhotoUrl = $"/uploads/students/{uniqueFileName}";
                }

                _context.Students.Update(studentInDb);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("SuccessMessage", "Student updated successfully!");
                return RedirectToAction("Dashboard");
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest("Unable to update student due to concurrency issue.");
            }
            catch (Exception ex)
            {
                // General error handling
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

        public async Task<IActionResult> Dashboard()
        {
           
                // 1. Authentication and Session Check
                var studentId = HttpContext.Session.GetInt32("StudentPkId");
                if (studentId == null)
                {
                    TempData["Error"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "StudentLogin");
                }

                // 2. Load Student with Related Data
                var student = await _context.Students
                    .Include(s => s.EmailPk)
                    .Include(s => s.DepartmentPk)
                    .Include(s => s.NrcPk)
                    .Include(s => s.NrctypePk)
                    .Include(s => s.AcademicYearPk)
                    .FirstOrDefaultAsync(s => s.StudentPkId == studentId);

                if (student == null)
                {
                    TempData["Error"] = "Student not found.";

                //var isLeader = await _context.ProjectMembers
                //.AnyAsync(pm =>
                //    pm.StudentPkId == studentId &&
                //    pm.Role == "Leader" &&
                //    pm.IsDeleted == false);

                //ViewBag.IsLeader = isLeader;

                return RedirectToAction("Login", "StudentLogin");
                }

                var projects = await _context.Projects
                    .Where(p => p.SubmittedByStudentPkId == studentId ||
                               p.ProjectMembers.Any(pm => pm.StudentPkId == studentId && pm.IsDeleted == false))
                    .Include(p => p.ProjectTypePk)
                    .Include(p => p.LanguagePk)
                    .Include(p => p.FrameworkPk)
                    .Include(p => p.CompanyPk)
                        .ThenInclude(c => c.CityPk)
                    .Include(p => p.ProjectFiles)
                    //.Include(p => p.ProjectMembers)
                    //.ThenInclude(pm => pm.Student)
                    //.ThenInclude(s => s.Email)
                    //.Include(p => p.ProjectMembers)
                    //    .ThenInclude(pm => pm.Student)
                    //        .ThenInclude(s => s.StudentDepartment)
                    .OrderByDescending(p => p.ProjectSubmittedDate)
                    .AsNoTracking()
                    .ToListAsync();

                // 4. Load All Team Members for Leader Projects
                var leaderProjectIds = projects
                    .Where(p => p.SubmittedByStudentPkId == studentId)
                    .Select(p => p.ProjectPkId)
                    .ToList();

                var allTeamMembers = await _context.ProjectMembers
                    .Where(pm => leaderProjectIds.Contains((int)pm.ProjectPkId) && pm.IsDeleted == false)
                    .Include(pm => pm.StudentPk)
                        .ThenInclude(s => s.EmailPk)
                    .Include(pm => pm.StudentPk)
                        .ThenInclude(s => s.DepartmentPk)
                    .AsNoTracking()
                    .ToListAsync();

                // 5. Mark Leader Role in Teams
                foreach (var project in projects.Where(p => p.SubmittedByStudentPkId == studentId))
                {
                    var leaderMember = allTeamMembers.FirstOrDefault(m =>
                        m.ProjectPkId == project.ProjectPkId &&
                        m.StudentPkId == studentId);

                    if (leaderMember != null)
                    {
                        leaderMember.Role = "Leader";
                        //leaderMember.RoleDescription = leaderMember.RoleDescription ?? "Manage project and assign members";
                    }
                }
            var leaderProject = projects.FirstOrDefault(p => p.SubmittedByStudentPkId == studentId);
            ViewBag.IsLeader = leaderProject != null;
            ViewBag.LeaderProjectId = leaderProject?.ProjectPkId;

            var isSubmissionBlocked = await _context.Announcements
                .AnyAsync(a => a.IsActive == true && a.BlocksSubmissions == true);

            ViewBag.IsSubmissionBlocked = isSubmissionBlocked;

            // 6. Calculate Submission Status
            var submissionStatus = new ProjectSubmissionStatus
                {
                    TotalProjects = projects.Count,
                    DraftProjects = projects.Count(p => p.Status == "Draft"),
                    PendingProjects = projects.Count(p => p.Status == "Pending"),
                    ApprovedProjects = projects.Count(p => p.Status == "Approved"),
                    RejectedProjects = projects.Count(p => p.Status == "Rejected"),
                    Cancelled = projects.Count(p => p.Status == "Cancelled")
                };
            var notifications = await _context.Notifications
            .Where(n => n.UserId == student.StudentPkId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationViewModel
            {
                Id = n.NotificationPkId,
                Title = n.Title ?? "",
                Message = n.Message ?? "",
                NotificationType = n.NotificationType ?? "",
                CreatedAt = n.CreatedAt ?? DateTime.Now,
                IsRead = n.IsRead ?? false,

                ProjectId = n.ProjectPkId,   // ⭐ FIX

                ProjectName = n.ProjectPk != null
                    ? n.ProjectPk.ProjectName
                    : "No Project"               // ⭐ FIX

            })
            .ToListAsync();
            //var notifications = await _context.Notifications
            //  .Where(n => n.UserId == student.StudentPkId)
            //  .OrderByDescending(n => n.CreatedAt)
            //  .ToListAsync();

            // 7. Determine if the logged-in student is a member
            var memberInfo = await _context.ProjectMembers
                    .Include(pm => pm.ProjectPk)
                    .ThenInclude(p => p.ProjectMembers)
                        .ThenInclude(m => m.StudentPk)
                        .ThenInclude(s => s.EmailPk)
                    .FirstOrDefaultAsync(pm => pm.StudentPkId == studentId && pm.IsDeleted == false);

                bool isMember = false;
                string? memberProjectName = null;
                string? memberResponsibility = null;
                string? leaderName = null;
                string? leaderEmail = null;

                if (memberInfo != null && memberInfo.ProjectPk != null)
                {
                    isMember = true;
                    memberProjectName = memberInfo.ProjectPk.ProjectName;
                    memberResponsibility = memberInfo.RoleDescription;

                    // Find the Leader of this project
                    var leader = memberInfo.ProjectPk.ProjectMembers
                        .FirstOrDefault(m => m.Role == "Leader" && m.IsDeleted == false);

                    if (leader != null)
                    {
                        leaderName = leader.StudentPk?.StudentName;
                        leaderEmail = leader.StudentPk?.EmailPk?.EmailAddress;
                    }
                }

            var activeAnnouncements = await _context.Announcements
            .Where(a => a.IsActive == true)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

            ViewBag.ActiveAnnouncements = activeAnnouncements;

            var dashboardViewModel = new StudentDashboardViewModel
                {
                    Student = student,
                    Projects = projects,
                    TeamMembers = allTeamMembers,
                    SubmissionStatus = submissionStatus,
                    Notifications = notifications,
                    LeaderProjects = projects.Where(p => p.SubmittedByStudentPkId == studentId).ToList(),
                    IsMember = isMember,
                    MemberProjectName = memberProjectName,
                    MemberResponsibility = memberResponsibility,
                    LeaderName = leaderName,
                    LeaderEmail = leaderEmail
                };

                return View(dashboardViewModel);
            
        }

     


        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students
                .Include(s => s.DepartmentPk)
                .Include(s => s.ProjectMembers)
                .Include(s => s.NrcPk)
                .Include(s => s.NrctypePk)
                .Where(s => !s.IsDeleted== false)
                .FirstOrDefaultAsync(s => s.StudentPkId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student); // Show confirmation page
        }

        // POST: Student/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            student.IsDeleted = true;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("SuccessMessage", "Student removed successfully!");
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Help()
        {
            // You can pass data to the view if needed, for now just return the view
            return View();
        }
    }
}
