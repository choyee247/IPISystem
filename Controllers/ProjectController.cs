using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;


namespace ProjectManagementSystem.Controllers
{
    public class ProjectController : BaseStudentController
    {
        private readonly PMSDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProjectController> _logger;
        private object studentName;
        private readonly IEmailService _emailService;

        public ProjectController(PMSDbContext context, IWebHostEnvironment env, ILogger<ProjectController> logger , IEmailService emailService)
             : base(context)
        {
            _emailService = emailService;
            _context = context;
            _env = env;
            _logger = logger;           
        }

        //   public async Task<IActionResult> Index(int page = 1)
        //   {
        //       var rollNumber = HttpContext.Session.GetString("RollNumber");
        //       if (string.IsNullOrEmpty(rollNumber))
        //       {
        //           return RedirectToAction("Login", "StudentLogin");
        //       }

        //       int pageSize = 3;
        //       var projects = _context.Projects
        //.Include(p => p.ProjectType)
        //.Include(p => p.Language)
        //.Include(p => p.Framework)
        //.Include(p => p.Company)
        //.Include(p => p.Files)
        //.Include(p => p.ProjectMembers)
        //    .ThenInclude(pm => pm.Student)
        //.OrderByDescending(p => p.ProjectSubmittedDate);


        //       var pagedProjects = await projects.ToPagedListAsync(page, pageSize);

        //       return View(pagedProjects);
        //   }

        //public async Task<IActionResult> Index(int page = 1)
        //{
        //    var rollNumber = HttpContext.Session.GetString("RollNumber");
        //    if (string.IsNullOrEmpty(rollNumber))
        //    {
        //        return RedirectToAction("Login", "StudentLogin");
        //    }

        //    int pageSize = 1; // Show only one project per page
        //    var projects = _context.Projects
        //        .Include(p => p.ProjectTypePk)
        //        .Include(p => p.LanguagePk)
        //        .Include(p => p.FrameworkPk)
        //        .Include(p => p.CompanyPk)
        //        .Include(p => p.ProjectFiles)
        //        .Include(p => p.ProjectMembers)
        //            .ThenInclude(pm => pm.StudentPk)
        //        .Where(p => p.ProjectMembers.Any(pm => pm.StudentPk.EmailPk.RollNumber == rollNumber)) // Filter by current student
        //        .OrderByDescending(p => p.ProjectSubmittedDate);

        //    var pagedProjects = await projects.ToPagedListAsync(page, pageSize);


        //    return View(pagedProjects);
        //}
        public async Task<IActionResult> Index(int page = 1)
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return RedirectToAction("Login", "StudentLogin");

            int pageSize = 3; // 5 projects per page

            var projects = _context.Projects
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.FrameworkPk)
                .Include(p => p.CompanyPk)
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                        .ThenInclude(s => s.EmailPk)
                .Where(p => p.ProjectMembers
                    .Any(pm => pm.StudentPk.EmailPk.RollNumber.ToLower() == rollNumber.ToLower()))
                .OrderByDescending(p => p.ProjectSubmittedDate);

            var pagedProjects = await projects.ToPagedListAsync(page, pageSize);

            return View(pagedProjects);
        }


        [HttpGet]
        public JsonResult GetSuggestions(string term)
        {
            var suggestions = _context.Projects
                .Where(p => p.ProjectName.Contains(term))
                .OrderBy(p => p.ProjectName)
                .Select(p => p.ProjectName)
                .Distinct()
                .Take(5)
                .ToList();

            return Json(suggestions);
        }

        // GET: Project/Upload/5
        //public async Task<IActionResult> Upload(int id)
        //{
        //    var project = await _context.Projects
        //        .Include(p => p.ProjectType)
        //        .Include(p => p.Language)
        //        .Include(p => p.Framework)
        //        .Include(p => p.Company)
        //        .Include(p => p.Files)
        //        .FirstOrDefaultAsync(p => p.Project_pkId == id);

        //    if (project == null)
        //        return NotFound();

        //    if (project.Status == "Pending" || project.Status == "Approved")
        //    {
        //        TempData["UploadError"] = "You cannot upload again because the project is already submitted or approved.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(project);
        //}


        [HttpGet]
        // GET: Project/Upload/5
        public async Task<IActionResult> Upload(int id)
        {
            // Check if submissions are blocked
            var activeBlock = _context.Announcements
            .Any(a => a.BlocksSubmissions == true && a.IsActive == true);

            if (activeBlock)
            {
                TempData["Error"] = "🚫 Project submissions are temporarily blocked by the teacher.";
                return RedirectToAction("Dashboard", "Student"); 
            }

            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                return RedirectToAction("Login", "StudentLogin");
            }
            var project = await _context.Projects
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.FrameworkPk)
                .Include(p => p.CompanyPk)
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null)
                return NotFound();

            if (project.Status == "Pending" || project.Status == "Approved")
            {
                TempData["UploadError"] = "You cannot upload again because the project is already submitted or approved.";
                return RedirectToAction(nameof(Index));
            }
            HttpContext.Session.SetInt32("CurrentStep", 5);
            return View(project);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(DBModels.Project project, List<IFormFile> projectFiles)
        {
            var existingProject = await _context.Projects
                .Include(p => p.LanguagePk)
                .Include(p => p.ProjectTypePk)
                .Include(p => p.FrameworkPk)
                .Include(p => p.CompanyPk)
                .Include(p => p.Teacher)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == project.ProjectPkId);

            if (existingProject == null)
                return NotFound();

            // 🚫 Prevent re-submission
            if (existingProject.Status == "Pending" || existingProject.Status == "Approved")
            {
                TempData["Error"] = "You cannot upload again. Wait for teacher feedback.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Update project status
            existingProject.Status = "Pending";
            existingProject.ProjectSubmittedDate = DateTime.Now;

            // ---------------- FILE UPLOAD ----------------
            if (projectFiles != null && projectFiles.Any())
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "projects");
                Directory.CreateDirectory(uploadsFolder);

                long maxFileSize = 100 * 1024 * 1024; // 100MB

                foreach (var file in projectFiles)
                {
                    if (file == null || file.Length == 0)
                        continue;

                    if (file.Length > maxFileSize)
                    {
                        TempData["Error"] = $"File {file.FileName} exceeds 100MB size limit.";
                        return RedirectToAction(nameof(Upload), new { id = project.ProjectPkId });
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.ProjectFiles.Add(new DBModels.ProjectFile
                    {
                        ProjectPkId = existingProject.ProjectPkId,
                        FilePath = $"/uploads/projects/{uniqueFileName}",
                        FileType = Path.GetExtension(file.FileName),
                        FileSize = file.Length,
                        UploadedAt = DateTime.Now
                    });
                }
            }

            // ---------------- NOTIFICATIONS ----------------

            var leaderName = existingProject.ProjectMembers
                .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk?.StudentName ?? "Unknown Leader";

            // Notify project members
            foreach (var member in existingProject.ProjectMembers)
            {
                _context.Notifications.Add(new DBModels.Notification
                {
                    UserId = member.StudentPk.StudentPkId,
                    Title = "Project Submitted",
                    Message = $"Leader {leaderName} submitted the project '{existingProject.ProjectName}'.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    NotificationType = "ProjectSubmitted",
                    ProjectPkId = existingProject.ProjectPkId
                });
            }

            string? teacherEmail = null;
            string? teacherName = null;

            // Notify Teacher
            if (existingProject.Teacher != null)
            {
                teacherEmail = existingProject.Teacher.Email;
                teacherName = existingProject.Teacher.FullName;

                _context.Notifications.Add(new DBModels.Notification
                {
                    UserId = existingProject.Teacher.Id,
                    Title = "Project Submitted",
                    Message = $"Student {leaderName} submitted the project '{existingProject.ProjectName}'.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    NotificationType = "ProjectSubmitted",
                    ProjectPkId = existingProject.ProjectPkId
                });
            }

            // ✅ Save everything first
            await _context.SaveChangesAsync();

            // ---------------- EMAIL SECTION ----------------
            if (!string.IsNullOrWhiteSpace(teacherEmail))
            {
                var emailBody = $@"
Hello {teacherName},

This is to inform you that the project '{existingProject.ProjectName}' has been successfully submitted by the student team.

Team Leader: {leaderName}
Submission Date: {DateTime.Now:dd MMM yyyy hh:mm tt}

Please log in to the system to review, provide feedback, and approve or reject the submission.

Thank you for your guidance and support.

Best regards,
Project Management System
";
                if (_emailService != null && !string.IsNullOrWhiteSpace(teacherEmail))
                {
                    await _emailService.SendEmailAsync(
                        teacherEmail,
                        "New Project Submitted",
                        emailBody
                    );
                }

                //try
                //{
                //    await _emailService.SendEmailAsync(
                //        teacherEmail,
                //        "New Project Submitted",
                //        emailBody
                //    );
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine("Teacher email failed: " + ex.Message);
                //}
            }

            HttpContext.Session.SetInt32("CurrentStep", 6);
            TempData["Success"] = "Project submitted successfully and sent to teacher.";
            return RedirectToAction(nameof(Index));
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Upload(DBModels.Project project, List<IFormFile> projectFiles)
        //{
        //    var existingProject = await _context.Projects
        //        .Include(p => p.LanguagePk)
        //        .Include(p => p.ProjectTypePk)
        //        .Include(p => p.FrameworkPk)
        //        .Include(p => p.CompanyPk)
        //        .Include(p => p.ProjectMembers)
        //            .ThenInclude(pm => pm.StudentPk)
        //        .FirstOrDefaultAsync(p => p.ProjectPkId == project.ProjectPkId);

        //    if (existingProject == null) return NotFound();

        //    // Prevent multiple uploads
        //    if (existingProject.Status == "Pending" || existingProject.Status == "Approved")
        //    {
        //        TempData["Error"] = "You cannot upload again. Wait for teacher feedback.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    // Mark project as submitted
        //    existingProject.Status = "Pending";
        //    existingProject.ProjectSubmittedDate = DateTime.Now;

        //    // --- Handle uploaded files ---
        //    if (projectFiles != null && projectFiles.Any())
        //    {
        //        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "projects");
        //        Directory.CreateDirectory(uploadsFolder);

        //        long maxFileSize = 100 * 1024 * 1024; 

        //        foreach (var file in projectFiles)
        //        {
        //            if (file.Length == 0) continue;

        //            if (file.Length > maxFileSize)
        //            {
        //                TempData["Error"] = $"File {file.FileName} exceeds 100MB size limit.";
        //                return RedirectToAction(nameof(Upload), new { id = project.ProjectPkId });
        //            }

        //            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        //            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //            using var stream = new FileStream(filePath, FileMode.Create);
        //            await file.CopyToAsync(stream);

        //            _context.ProjectFiles.Add(new DBModels.ProjectFile
        //            {
        //                ProjectPkId = existingProject.ProjectPkId,
        //                FilePath = $"/uploads/projects/{uniqueFileName}",
        //                FileType = Path.GetExtension(file.FileName),
        //                FileSize = file.Length,
        //                UploadedAt = DateTime.Now
        //            });
        //        }
        //    }

        //    // --- Notifications ---
        //    var leader = existingProject.ProjectMembers
        //        .FirstOrDefault(pm => pm.Role == "Leader")?.StudentPk?.StudentName ?? "Unknown Leader";

        //    foreach (var member in existingProject.ProjectMembers)
        //    {
        //        var notification = new DBModels.Notification
        //        {
        //            UserId = member.StudentPk.StudentPkId,
        //            Title = "Project Submitted",
        //            Message = $"Leader {leader} submitted the project '{existingProject.ProjectName}'.",
        //            CreatedAt = DateTime.Now,
        //            IsRead = false,
        //            NotificationType = "ProjectSubmitted",
        //            ProjectPkId = existingProject.ProjectPkId,
        //            TeacherId = existingProject.TeacherId
        //        };

        //        _context.Notifications.Add(notification);
        //    }

        //    await _context.SaveChangesAsync();

        //    HttpContext.Session.SetInt32("CurrentStep", 6);
        //    TempData["UploadSuccess"] = "Project successfully uploaded and sent to teacher.";
        //    TempData["Success"] = "Project submitted successfully!";
        //    return RedirectToAction(nameof(Index));
        //}


        // Add similar notification logic for when a project is approved
        public async Task<IActionResult> ApproveProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null)
                return NotFound();

            project.Status = "Approved";

            // Create notification for each team member
            foreach (var member in project.ProjectMembers)
            {
                var notification = new DBModels.Notification
                {
                    UserId = member.StudentPk.StudentPkId,
                    Title = "Project Approved",
                    Message = $"Congratulations! Your project '{project.ProjectName}' has been approved.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    NotificationType = "ProjectStatus"
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                return RedirectToAction("Login", "StudentLogin");
            }

            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.FrameworkPk)
                .Include(p => p.CompanyPk)
                    .ThenInclude(c => c.CityPk)
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk) 
                .FirstOrDefaultAsync(m => m.ProjectPkId == id);

            if (project == null) return NotFound();

            return View(project);
        }
   
        [HttpGet]
        public IActionResult AddMember(int projectId)
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                return RedirectToAction("Login", "StudentLogin");
            }
            HttpContext.Session.SetInt32("CurrentStep", 4);
            try
            {
               
                var project = _context.Projects
                    .Include(p => p.ProjectTypePk)
                    .Include(p => p.LanguagePk)
                    .Include(p => p.FrameworkPk)
                    .FirstOrDefault(p => p.ProjectPkId == projectId);

                if (project == null)
                {
                    TempData["Error"] = "Project not found.";
                    return RedirectToAction("Dashboard", "Student");
                }

                var model = new AddMemberViewModel
                {
                    ProjectId = projectId,
                    ProjectName = project.ProjectName,
                    ProjectType = project.ProjectTypePk,
                    Language = project.LanguagePk,
                    Framework = project.FrameworkPk
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error loading AddMember page");
                TempData["Error"] = "An error occurred while loading the page.";
                return RedirectToAction("Dashboard", "Student");
            }
        }
        // POST: Project/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(AddMemberViewModel model)
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                return RedirectToAction("Login", "StudentLogin");
            }

            HttpContext.Session.SetInt32("CurrentStep", 5);

            // Find student by RollNumber and EmailAddress
            var student = await _context.Students
                .Include(s => s.EmailPk)
                .FirstOrDefaultAsync(s =>
                    s.EmailPk.RollNumber == model.RollNumber &&
                    s.EmailPk.EmailAddress == model.EmailAddress);

            if (student == null)
            {
                ModelState.AddModelError(string.Empty, "Student not found. Please check Roll Number and Email.");
                return View(model);
            }

            // Check if already added
            bool alreadyAdded = await _context.ProjectMembers
                .AnyAsync(pm => pm.StudentPkId == student.StudentPkId && pm.ProjectPkId == model.ProjectId);

            if (alreadyAdded)
            {
                ModelState.AddModelError(string.Empty, "This student is already added to the project.");
                return View(model);
            }

            // Add member
            var newMember = new DBModels.ProjectMember
            {
                StudentPkId = student.StudentPkId,
                ProjectPkId = model.ProjectId,
                Role = "Member",
                RoleDescription = model.RoleDescription, // Save member's work part
                IsDeleted = false
            };

            _context.ProjectMembers.Add(newMember);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Team member added successfully.";
            return RedirectToAction("Dashboard", "Student");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int projectId, int studentId)
        {
            // Session check
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "StudentLogin");
            }

            try
            {
                // Get current user and validate
                var currentUser = await _context.Students
                    .Include(s => s.EmailPk)
                    .FirstOrDefaultAsync(s => s.EmailPk.RollNumber == rollNumber);

                if (currentUser == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Login", "StudentLogin");
                }

                // Get project with members
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(pm => pm.StudentPk)
                            .ThenInclude(s => s.EmailPk)
                    .FirstOrDefaultAsync(p => p.ProjectPkId == projectId);

                if (project == null)
                {
                    TempData["Error"] = "Project not found.";
                    return RedirectToAction("Dashboard", "Student");
                }

                // Verify current user is the project leader
                var currentUserIsLeader = project.ProjectMembers
                    .Any(pm => pm.StudentPkId == currentUser.StudentPkId && pm.Role == "Leader");

                if (!currentUserIsLeader)
                {
                    TempData["Error"] = "Only project leaders can remove members.";
                    return RedirectToAction("Dashboard", "Student");
                }

                // Find member to remove
                var memberToRemove = await _context.ProjectMembers
                    .Include(pm => pm.StudentPk)
                    .FirstOrDefaultAsync(pm =>
                        pm.ProjectPkId == projectId &&
                        pm.StudentPkId == studentId &&
                        pm.Role != "Leader");

                if (memberToRemove == null)
                {
                    TempData["Error"] = "Member not found or cannot be removed (cannot remove leaders).";
                    return RedirectToAction("Dashboard", "Student");
                }

                // Prevent removing yourself
                if (memberToRemove.StudentPkId == currentUser.StudentPkId)
                {
                    TempData["Error"] = "You cannot remove yourself as leader. Transfer leadership first.";
                    return RedirectToAction("Dashboard", "Student");
                }

                // Remove member
                _context.ProjectMembers.Remove(memberToRemove);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{memberToRemove.StudentPk?.StudentName ?? "Member"} has been removed from the project.";
                return RedirectToAction("Dashboard", "Student");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while removing member");
                TempData["Error"] = "A database error occurred while removing the member.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error removing member");
                TempData["Error"] = "An unexpected error occurred while removing the member.";
            }

            return RedirectToAction("Dashboard", "Student");
        }


        //public IActionResult Create()
        //{
        //    var rollNumber = HttpContext.Session.GetString("RollNumber");
        //    if (string.IsNullOrEmpty(rollNumber))
        //    {
        //        return RedirectToAction("Login", "StudentLogin");
        //    }
        //    LoadDropdownData();
        //    return View();
        //}
        public async Task<IActionResult> Create()
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
                return RedirectToAction("Login", "StudentLogin");

            HttpContext.Session.SetInt32("CurrentStep", 3);

            var student = await _context.Students
                .Include(s => s.EmailPk)
                .FirstOrDefaultAsync(s =>
                    s.EmailPk.RollNumber == rollNumber &&
                    s.IsDeleted == false);

            if (student == null)
                return RedirectToAction("Dashboard", "Student");

            var project = new Project();

            // 🔑 Get active teacher assignment
            var teacherStudent = await _context.TeacherStudents
                .Include(ts => ts.Teacher)
                .ThenInclude(t => t.TeacherCompanies)
                    .ThenInclude(tc => tc.CompanyPk)
                .Where(ts =>
                    ts.StudentPkId == student.StudentPkId &&
                    ts.IsActive == true)
                .OrderByDescending(ts => ts.AssignedDate)
                .FirstOrDefaultAsync();

            if (teacherStudent != null)
            {
                project.TeacherId = teacherStudent.TeacherId;
                ViewBag.SupervisorName = teacherStudent.Teacher.FullName;

                // ✅ STEP 1 — Check StudentCompany first
                var studentCompany = await _context.StudentCompanies
                    .Include(sc => sc.Company)
                    .FirstOrDefaultAsync(sc =>
                        sc.StudentId == student.StudentPkId &&
                        sc.IsActive == true);

                if (studentCompany != null)
                {
                    // 🎯 Use student's assigned company
                    project.CompanyPkId = studentCompany.CompanyId;
                    ViewBag.CompanyName = studentCompany.Company.CompanyName;
                }
                else
                {
                    // 🔁 Fallback → teacher's first assigned company
                    var teacherCompany = teacherStudent.Teacher.TeacherCompanies
                        .Where(tc => tc.IsActive)
                        .Select(tc => tc.CompanyPk)
                        .FirstOrDefault();

                    project.CompanyPkId = teacherCompany?.CompanyPkId ?? 0;
                    ViewBag.CompanyName = teacherCompany?.CompanyName ?? "Not Assigned";
                }
            }
            else
            {
                ViewBag.SupervisorName = "Not Assigned";
                ViewBag.CompanyName = "Not Assigned";
            }

            LoadDropdownData();

            return View(project);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            DBModels.Project project,
            string CompanyAddress,
            string CompanyContact,
            string CompanyDescription,
            int CompanyCity_pkId,
            IFormFile CompanyPhoto,
            List<IFormFile> projectFiles)
        {
            // 1️⃣ Get logged-in student
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                return RedirectToAction("Login", "StudentLogin");
            }

            var student = await _context.Students
                .Include(s => s.EmailPk)
                .FirstOrDefaultAsync(s =>
                    s.EmailPk.RollNumber == rollNumber &&
                    s.IsDeleted == false);

            if (student == null)
            {
                TempData["Error"] = "Student not found in the system.";
                return RedirectToAction("Dashboard", "Student");
            }

            // 2️⃣ Get supervisor teacher from TeacherStudent
            var teacherStudent = await _context.TeacherStudents
                .Include(ts => ts.Teacher)
                .Where(ts =>
                    ts.StudentPkId == student.StudentPkId &&
                    ts.IsActive == true)
                .OrderByDescending(ts => ts.AssignedDate)
                .FirstOrDefaultAsync();

            if (teacherStudent == null)
            {
                TempData["Error"] = "Supervisor teacher is not assigned yet.";
                return RedirectToAction("Dashboard", "Student");
            }

            // 3️⃣ Prepare project data
            project.StudentPkId = student.StudentPkId;
            project.SubmittedByStudentPkId = student.StudentPkId;
            project.Status = "Draft";
            project.ProjectSubmittedDate = null;
            project.CreatedDate = DateTime.Now;

            project.TeacherId = teacherStudent.TeacherId;
            project.SupervisorName = teacherStudent.Teacher.FullName;

            // 4️⃣ Update company (if exists)
            if (project.CompanyPkId != 0)
            {
                var company = await _context.Companies.FindAsync(project.CompanyPkId);
                if (company != null)
                {
                    company.Address = CompanyAddress;
                    company.Contact = CompanyContact;
                    company.Description = CompanyDescription;
                    if (CompanyCity_pkId > 0)
                    {
                        company.CityPkId = CompanyCity_pkId;
                    }
                    if (CompanyPhoto != null && CompanyPhoto.Length > 0)
                    {
                        var companyFolder = Path.Combine(_env.WebRootPath, "images", "companies");
                        Directory.CreateDirectory(companyFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(CompanyPhoto.FileName)}";
                        var filePath = Path.Combine(companyFolder, uniqueFileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await CompanyPhoto.CopyToAsync(stream);

                        company.ImageFileName = uniqueFileName;
                    }

                    _context.Companies.Update(company);
                }
            }

            // 5️⃣ Save project
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // 6️⃣ Add leader to ProjectMembers
            var projectMember = new DBModels.ProjectMember
            {
                StudentPkId = student.StudentPkId,
                ProjectPkId = project.ProjectPkId,
                Role = "Leader",
                RoleDescription = "Manage project and assign members",
                IsDeleted = false
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync();

            // 7️⃣ Save project files
            if (projectFiles != null && projectFiles.Any())
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "projects");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in projectFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.ProjectFiles.Add(new DBModels.ProjectFile
                        {
                            ProjectPkId = project.ProjectPkId,
                            FilePath = $"/uploads/projects/{uniqueFileName}",
                            FileType = Path.GetExtension(file.FileName),
                            FileSize = file.Length,             
                            UploadedAt = DateTime.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // 8️⃣ Done
            TempData["Success"] = "Project created successfully.";
            HttpContext.Session.SetInt32("CurrentStep", 4);
            return RedirectToAction("Dashboard", "Student");
        }


        public async Task<IActionResult> Edit(int? id)
        {
            var rollNumber = HttpContext.Session.GetString("RollNumber");
            if (string.IsNullOrEmpty(rollNumber))
            {
                return RedirectToAction("Login", "StudentLogin");
            }

            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.FrameworkPk)
                .Include(p => p.CompanyPk)
                .Include(p => p.ProjectFiles)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null) return NotFound();
            if (project.Status == "Pending" || project.Status == "Approved")
            {
                TempData["Error"] = "You cannot edit a project that is pending or approved. Wait for teacher feedback.";
                return RedirectToAction(nameof(Index));
            }

            LoadDropdownData(project); // ✅ required
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            DBModels.Project project,
            string CompanyAddress,
            string CompanyContact,
            string CompanyDescription,
            int CompanyCity_pkId,
            IFormFile CompanyPhoto,
            List<IFormFile> projectFiles)
        {
            if (id != project.ProjectPkId)
                return NotFound();
            var existingProject = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectPkId == id);
            if (existingProject == null)
                return NotFound();


            if (existingProject.Status == "Pending" || existingProject.Status == "Approved")
            {
                TempData["Error"] = "You cannot edit a project that is pending or approved. Wait for teacher feedback.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                LoadDropdownData(project);
                return View(project);
            }

            try
            {
                // Fetch the existing project with related data
                var projectInDb = await _context.Projects
                    .Include(p => p.CompanyPk)
                    .Include(p => p.ProjectFiles)
                    .FirstOrDefaultAsync(p => p.ProjectPkId == id);

                if (projectInDb == null)
                    return NotFound();

                // Update project fields explicitly
                projectInDb.ProjectName = project.ProjectName;
                projectInDb.SupervisorName = project.SupervisorName;
                projectInDb.Description = project.Description;
                projectInDb.ProjectTypePkId = project.ProjectTypePkId;
                projectInDb.LanguagePkId = project.LanguagePkId;
                projectInDb.FrameworkPkId = project.FrameworkPkId;
                projectInDb.CompanyPkId = project.CompanyPkId;
                projectInDb.ProjectSubmittedDate = project.ProjectSubmittedDate;
                projectInDb.CreatedBy = project.CreatedBy;

                // Update company info if company selected
                if (project.CompanyPkId != null && project.CompanyPkId != 0)
                {
                    var company = await _context.Companies.FindAsync(project.CompanyPkId);
                    if (company != null)
                    {
                        company.Address = CompanyAddress;
                        company.Contact = CompanyContact;
                        company.Description = CompanyDescription;
                        company.CityPkId = CompanyCity_pkId;

                        // Handle company photo upload
                        if (CompanyPhoto != null && CompanyPhoto.Length > 0)
                        {
                            var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "companies");
                            Directory.CreateDirectory(uploadsFolder);

                            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(CompanyPhoto.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using var stream = new FileStream(filePath, FileMode.Create);
                            await CompanyPhoto.CopyToAsync(stream);

                            company.ImageFileName = uniqueFileName;
                        }

                        _context.Companies.Update(company);
                    }
                }

                _context.Projects.Update(projectInDb);
                await _context.SaveChangesAsync();

                // Handle project files upload
                if (projectFiles != null && projectFiles.Count > 0)
                {
                    var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "projects");
                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in projectFiles)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            var filePath = Path.Combine(uploadPath, uniqueFileName);

                            using var stream = new FileStream(filePath, FileMode.Create);
                            await file.CopyToAsync(stream);

                            var projectFile = new DBModels.ProjectFile
                            {
                                ProjectPkId = projectInDb.ProjectPkId,
                                FilePath = $"/uploads/projects/{uniqueFileName}",
                                FileType = Path.GetExtension(file.FileName)
                            };

                            _context.ProjectFiles.Add(projectFile);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Projects.Any(e => e.ProjectPkId == project.ProjectPkId))
                    return NotFound();
                else
                    throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubmit(int id)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null) return NotFound();

            project.Status = "Pending";
            project.AdminComment = null;
            project.ProjectSubmittedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .Include(p => p.Notifications) // <--- include notifications
                .FirstOrDefaultAsync(p => p.ProjectPkId == id);

            if (project == null)
                return NotFound();

            // Remove related notifications first
            if (project.Notifications.Any())
            {
                _context.Notifications.RemoveRange(project.Notifications);
            }

            // Remove project members
            if (project.ProjectMembers.Any())
            {
                _context.ProjectMembers.RemoveRange(project.ProjectMembers);
            }

            // Remove project itself
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteProjectFile(int id)
        {
            var file = await _context.ProjectFiles.FindAsync(id);
            if (file == null) return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.ProjectFiles.Remove(file);
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new { id = file.ProjectPkId });
        }
        public async Task<IActionResult> ManageLeader(int projectId)
        {
            var studentId = HttpContext.Session.GetInt32("StudentPkId");
            if (studentId == null) return RedirectToAction("Login", "StudentLogin");

            var isLeader = await _context.ProjectMembers
                .AnyAsync(pm =>
                    pm.StudentPkId == studentId &&
                    pm.ProjectPkId == projectId &&
                    pm.Role.ToLower() == "leader" &&
                    pm.IsDeleted == false);

            if (!isLeader) return Unauthorized();

            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.StudentPk)
                        .ThenInclude(pm => pm.EmailPk)
                .FirstOrDefaultAsync(p => p.ProjectPkId == projectId);

            if (project == null) return NotFound();

            return View(project); // Create ManageLeader.cshtml view
        }
        [HttpPost]
        public async Task<IActionResult> TransferLeadership(int projectId, int newLeaderId)
        {
            var studentId = HttpContext.Session.GetInt32("StudentPkId");
            if (studentId == null)
                return RedirectToAction("Login", "StudentLogin");

            var currentLeader = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectPkId == projectId &&
                    pm.StudentPkId == studentId &&
                    pm.Role == "Leader" &&
                    pm.IsDeleted == false);

            if (currentLeader == null)
                return Unauthorized();

            var newLeader = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectPkId == projectId &&
                    pm.StudentPkId == newLeaderId &&
                    pm.IsDeleted == false);

            if (newLeader == null)
                return BadRequest();

            currentLeader.Role = "Member";
            newLeader.Role = "Leader";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leadership transferred successfully.";
            return RedirectToAction("ManageLeader", new { projectId });
        }

        [HttpPost]
        public async Task<IActionResult> TransferAndLeave(int projectId, int newLeaderId)
        {
            var studentId = HttpContext.Session.GetInt32("StudentPkId");
            if (studentId == null)
                return RedirectToAction("Login", "StudentLogin");

            var currentLeader = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectPkId == projectId &&
                    pm.StudentPkId == studentId &&
                    pm.Role == "Leader" &&
                    pm.IsDeleted == false);

            if (currentLeader == null)
                return Unauthorized();

            var newLeader = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectPkId == projectId &&
                    pm.StudentPkId == newLeaderId &&
                    pm.IsDeleted == false);

            if (newLeader == null)
                return BadRequest();

            newLeader.Role = "Leader";
            currentLeader.IsDeleted = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leadership transferred and you left the project.";
            return RedirectToAction("Dashboard", "Student");
        }
        [HttpGet]
        public JsonResult GetLanguagesByProjectType(int projectTypeId)
        {
            var languages = _context.Languages
                .Where(l => l.ProjectTypePkId == projectTypeId)
                .OrderBy(l => l.LanguageName)
                .Select(l => new
                {
                    language_pkId = l.LanguagePkId,
                    languageName = l.LanguageName
                })
                .ToList();

            return Json(languages);
        }

        [HttpGet]
        public JsonResult GetFrameworksByLanguage(int languageId)
        {
            var frameworks = _context.Frameworks
                .Where(f => f.LanguagePkId == languageId)
                .OrderBy(f => f.FrameworkName)
                .Select(f => new
                {
                    framework_pkId = f.FrameworkPkId,
                    frameworkName = f.FrameworkName
                })
                .ToList();

            return Json(frameworks);
        }

        [HttpGet]
        public async Task<JsonResult> GetCompanyInfo(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return Json(null);

            return Json(new
            {
                address = company.Address,
                contact = company.Contact,
                description = company.Description,
                city_pkId = company.CityPkId,
                imageFileName = company.ImageFileName
            });
        }

        private void LoadDropdownData(DBModels.Project? selectedProject = null)
        {
            ViewBag.ProjectTypes = new SelectList(
                _context.ProjectTypes.OrderBy(p => p.TypeName),
                "ProjectTypePkId",
                "TypeName",
                selectedProject?.ProjectTypePkId
            );

            // ✅ Show only related languages for selected project type
            if (selectedProject?.ProjectTypePkId != null)
            {
                var relatedLanguages = _context.Languages
                    .Where(l => l.ProjectTypePkId == selectedProject.ProjectTypePkId)
                    .OrderBy(l => l.LanguageName)
                    .ToList();

                ViewBag.Languages = new SelectList(
                    relatedLanguages,
                    "LanguagePkId",
                    "LanguageName",
                    selectedProject.LanguagePkId
                );
            }
            else
            {
                ViewBag.Languages = new SelectList(
                    Enumerable.Empty<SelectListItem>(),
                    "LanguagePkId",
                    "LanguageName"
                );
            }

            ViewBag.Languages = new SelectList(
                _context.Languages.OrderBy(l => l.LanguageName),
                "LanguagePkId",
                "LanguageName",
                selectedProject?.LanguagePkId
              );

            ViewBag.Frameworks = new SelectList(
                _context.Frameworks.OrderBy(f => f.FrameworkName),
                "FrameworkPkId",
                "FrameworkName",
                selectedProject?.FrameworkPkId
            );

            ViewBag.Companies = new SelectList(
                _context.Companies
                    .Where(c => !string.IsNullOrEmpty(c.CompanyName))
                    .OrderBy(c => c.CompanyName),
                "CompanyPkId",
                "CompanyName",
                selectedProject?.CompanyPkId
            );

            ViewBag.CityList = new SelectList(
                _context.Cities.OrderBy(c => c.CityName),
                "CityPkId",
                "CityName"
            );
        }

        private async Task<bool> IsSubmissionBlocked()
        {
            return await _context.Announcements
                .AnyAsync(a => a.IsActive==false &&
                       a.BlocksSubmissions== false &&
                       DateTime.Now >= a.StartDate &&
                       (a.ExpiryDate == null || DateTime.Now <= a.ExpiryDate));
        }

    }
}