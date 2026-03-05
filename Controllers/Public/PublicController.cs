using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MimeKit.Text;
using MimeKit;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.ViewModels;
using X.PagedList;
using System.Net.Mail;
using MailKit.Net.Smtp;
using System.IO.Compression;
using ProjectManagementSystem.Models.Public;

namespace ProjectManagementSystem.Controllers.Public
{
    public class PublicController : Controller
    {
        private readonly PMSDbContext _context;
        private const string AccessCode = "OLDSTUDENT2025"; // your secret code
        private readonly string _imageFolder = "uploads/successstories";
        private readonly IWebHostEnvironment _env;
        private int id;

        public PublicController(IWebHostEnvironment env,PMSDbContext context)
        {
            _env = env;
            _context = context;
        }
        public async Task<IActionResult> ProjectIdeas(
        int? projectTypeId,
        int? languageId,
        string? searchTerm,
        int? academicYearId,
        int page = 1)
        {
            var projectsQuery = _context.Projects
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.ProjectFiles)
                .Include(p => p.StudentPk) // Student join
                    .ThenInclude(s => s.AcademicYearPk) // Academic Year join
                .Include(p => p.CompanyPk) // 🔹 Company join
                .Where(p => (bool)!p.IsDeleted && p.Status == "Approved");

            if (projectTypeId.HasValue)
                projectsQuery = projectsQuery.Where(p => p.ProjectTypePkId == projectTypeId);

            if (languageId.HasValue)
                projectsQuery = projectsQuery.Where(p => p.LanguagePkId == languageId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectName.Contains(searchTerm) ||
                    p.Description.Contains(searchTerm));

            if (academicYearId.HasValue)
                projectsQuery = projectsQuery.Where(p => p.StudentPk.AcademicYearPkId == academicYearId);

            var totalProjects = await projectsQuery.CountAsync();
            var pageSize = 6;

            var paginatedProjects = await projectsQuery
                .OrderByDescending(p => p.ProjectPkId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var academicYears = await _context.AcademicYears
                .Where(ay => (bool)ay.IsActive)
                .Select(ay => new SelectListItem
                {
                    Value = ay.AcademicYearPkId.ToString(),
                    Text = ay.YearRange
                }).ToListAsync();

            // Get all requests for the current project files
            //var allRequests = await _context.DownloadRequests
            //    .Include(r => r.ProjectFilePk)
            //    .ToListAsync();

            //// Prepare dictionary: Key = ProjectFilePkId, Value = latest request
            //ViewBag.RequestStatuses = allRequests
            //    .GroupBy(r => r.ProjectFilePkId)
            //    .ToDictionary(
            //        g => g.Key,
            //        g => g.OrderByDescending(r => r.RequestDate).FirstOrDefault()
            //    );

            var studentId = HttpContext.Session.GetInt32("StudentId");

            if (studentId != null)
            {
                var approvedFiles = await _context.DownloadRequests
                    .Where(r => r.StudentPkId == studentId && r.IsApproved == true)
                    .Select(r => r.ProjectFilePkId)
                    .ToListAsync();

                ViewBag.ApprovedFiles = approvedFiles;
            }
            // Latest request per project
            var projectRequests = await _context.DownloadRequests
                .Where(r => paginatedProjects.Select(p => p.ProjectPkId).Contains(r.ProjectFilePk.ProjectPkId)
                            && (studentId == null || r.StudentPkId == studentId))
                .Include(r => r.ProjectFilePk)
                .ToListAsync();

            ViewBag.ProjectRequestStatuses = projectRequests
                .GroupBy(r => r.ProjectFilePk.ProjectPkId)   // Project-level
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.RequestDate).FirstOrDefault()
                );

            var projectFileIds = paginatedProjects
            .SelectMany(p => p.ProjectFiles)
            .Select(f => f.ProjectFilePkId)
            .ToList();

            var allRequests = await _context.DownloadRequests
                .Where(r => projectFileIds.Contains(r.ProjectFilePkId)
                            && (studentId == null || r.StudentPkId == studentId))
                .Include(r => r.ProjectFilePk)
                .ToListAsync();

            // Latest request per file
            ViewBag.RequestStatuses = allRequests
                .GroupBy(r => r.ProjectFilePkId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.RequestDate).FirstOrDefault()
                );

            var viewModel = new ProjectIdeasViewModel
            {
                Projects = paginatedProjects,
                ProjectTypes = await _context.ProjectTypes
                    .Select(pt => new SelectListItem
                    {
                        Value = pt.ProjectTypePkId.ToString(),
                        Text = pt.TypeName
                    }).ToListAsync(),
                Languages = languageId.HasValue
                    ? await _context.Languages
                        .Where(l => l.ProjectTypePkId == projectTypeId)
                        .Select(l => new SelectListItem
                        {
                            Value = l.LanguagePkId.ToString(),
                            Text = l.LanguageName
                        }).ToListAsync()
                    : new List<SelectListItem>(),
                AcademicYears = academicYears,
                SelectedProjectTypeId = projectTypeId,
                SelectedLanguageId = languageId,
                SelectedAcademicYearId = academicYearId,
                SearchTerm = searchTerm,
                TotalProjects = totalProjects,
                CurrentPage = page
            };

            return View(viewModel);
        }
        public IActionResult RequestDownload(int fileId)
        {
            if (fileId == 0)
                return BadRequest("Invalid FileId");

            var model = new DownloadRequest
            {
                ProjectFilePkId = fileId
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDownload(DownloadRequest model)
        {
            // Validate ProjectFilePkId
            if (model.ProjectFilePkId == 0)
                return BadRequest("Invalid file.");

            // Optionally, check file exists
            var fileExists = await _context.ProjectFiles
                .AnyAsync(f => f.ProjectFilePkId == model.ProjectFilePkId);

            if (!fileExists)
                return NotFound("File does not exist.");

            // Fill other fields
            model.StudentPkId = null; // For anonymous request
            model.RequestDate = DateTime.Now;
            model.IsApproved = null;
            model.IsBlocked = false;

            _context.DownloadRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Request submitted successfully.";
            return RedirectToAction("ProjectIdeas");
        }
        public async Task<IActionResult> DownloadProject(int requestId)
        {
            var request = await _context.DownloadRequests
                .Include(r => r.ProjectFilePk)
                    .ThenInclude(f => f.ProjectPk)
                        .ThenInclude(p => p.ProjectFiles)
                .FirstOrDefaultAsync(r => r.DownloadRequestPkId == requestId);

            if (request == null || request.IsApproved != true)
                return Unauthorized();

            // Log
            _context.DownloadTransactions.Add(new DownloadTransaction
            {
                DownloadRequestPkId = requestId,
                DownloadDate = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            var projectFiles = request.ProjectFilePk.ProjectPk.ProjectFiles;

            using var memoryStream = new MemoryStream();
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in projectFiles)
                {
                    var fullPath = Path.Combine(_env.WebRootPath,
                        file.FilePath.TrimStart('/'));

                    if (System.IO.File.Exists(fullPath))
                    {
                        var entry = zip.CreateEntry(Path.GetFileName(fullPath));
                        using var entryStream = entry.Open();
                        using var fileStream = System.IO.File.OpenRead(fullPath);
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }

            memoryStream.Position = 0;

            return File(memoryStream.ToArray(),
                        "application/zip",
                        "ProjectFiles.zip");
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<string>());

            var suggestions = await _context.Projects
                .Where(p => (bool)!p.IsDeleted && p.ProjectName.Contains(term))
                .OrderBy(p => p.ProjectName)
                .Select(p => p.ProjectName)
                .Distinct()
                .Take(10)
                .ToListAsync();

            return Json(suggestions);
        }


        [HttpGet]
        public async Task<IActionResult> GetLanguagesByProjectTypeUsedInProjects(int projectTypeId)
        {
            var languages = await _context.Languages
                .Where(l => l.ProjectTypePkId == projectTypeId)
                .Select(l => new
                {
                    value = l.LanguagePkId,
                    text = l.LanguageName
                }).ToListAsync();

            return Json(languages);
        }                      
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        // POST: /Public/Contact       
        public IActionResult Guidelines()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult Help()
        {
            // Return Help view
            return View();
        }
        public async Task<IActionResult> InternshipCompanies(int page = 1)
        {
            int pageSize = 5;

            var cities = await _context.Cities
                .Where(c => c.Companies
                    .Any(co => co.Projects
                        .Any(p => p.ProjectMembers.Any())))
                .Select(c => new CityInternshipViewModel
                {
                    CityPkId = c.CityPkId,
                    CityName = c.CityName ?? "",
                    ImageFileName = c.ImageFileName ?? "",
                    TotalStudents = c.Companies
                        .SelectMany(co => co.Projects)
                        .SelectMany(p => p.ProjectMembers)
                        .Count()
                })
                .OrderByDescending(c => c.TotalStudents)
                .ToListAsync();

            var pagedList = cities.ToPagedList(page, pageSize);

            var viewModel = new CityListViewModel
            {
                Cities = pagedList,
                CurrentPage = page
            };

            return View(viewModel);
        }



        // New action: list cities with images and Explore links
        public async Task<IActionResult> CompaniesByCity(int? id, int page = 1)
        {
            if (id == null) return NotFound();

            int pageSize = 3;

            // Load city and its companies with projects and project members
            var city = await _context.Cities
                .Include(c => c.Companies)
                    .ThenInclude(comp => comp.Projects)
                        .ThenInclude(p => p.ProjectMembers)
                .FirstOrDefaultAsync(c => c.CityPkId == id);

            if (city == null) return NotFound();

            // Map to CompanyViewModel including StudentCount and other fields
            var companyList = city.Companies
                .Select(c => new CompanyViewModel
                {
                    Company_pkId = c.CompanyPkId,
                    CompanyName = c.CompanyName ?? "",
                    StudentCount = c.Projects.Sum(p => p.ProjectMembers.Count),
                    Address = c.Address ?? "",
                    Contact = c.Contact ?? "",
                    Description = c.Description ?? "",
                    ImageFileName = c.ImageFileName ?? ""
                })
                .OrderBy(c => c.CompanyName)
                .ToPagedList(page, pageSize);

            ViewBag.CityName = city.CityName;
            ViewBag.CityId = city.CityPkId;
            ViewBag.TotalCompanies = city.Companies.Count;
            ViewBag.TotalStudents = city.Companies
        .SelectMany(c => c.Projects)
        .SelectMany(p => p.ProjectMembers)
        .Count();

            return View(companyList);
        }
        public async Task<IActionResult> Index()
        {
            // 🔹 LOAD ONLY APPROVED PROJECTS
            var approvedProjects = await _context.Projects
                .Where(p => p.Status == "Approved")   // ⭐ FILTER HERE
                .Include(p => p.LanguagePk)
                .Include(p => p.ProjectTypePk)
                .ToListAsync();

            // 🔹 GROUP LANGUAGES (APPROVED ONLY)
            var languageGroups = approvedProjects
                .Where(p => p.LanguagePk != null)
                .GroupBy(p => p.LanguagePk.LanguageName.Trim().ToLower())
                .Select(g => new
                {
                    LanguageName = Capitalize(g.Key),
                    Count = g.Count()
                })
                .ToList();

            // 🔹 GROUP PROJECT TYPES (APPROVED ONLY)
            var projectTypeGroups = approvedProjects
                .Where(p => p.ProjectTypePk != null)
                .GroupBy(p => p.ProjectTypePk.TypeName)
                .Select(g => new
                {
                    TypeName = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // 🔹 DASHBOARD VIEW MODEL
            var viewModel = new DashboardViewModel
            {
                // ✅ APPROVED PROJECT COUNT
                ProjectCount = approvedProjects.Count,

                LanguageCount = languageGroups.Count,
                LanguageNames = languageGroups.Select(x => x.LanguageName).ToList(),
                LanguageCounts = languageGroups.Select(x => x.Count).ToList(),

                ProjectTypeCount = projectTypeGroups.Count,
                ProjectTypeChartLabels = projectTypeGroups.Select(x => x.TypeName).ToList(),
                ProjectTypeChartValues = projectTypeGroups.Select(x => x.Count).ToList(),

                // ✅ APPROVED POPULAR PROJECTS
                PopularProjects = approvedProjects
                    .OrderByDescending(p => p.ProjectPkId)
                    .Take(6)
                    .Select(p => new ProjectIdea
                    {
                        Title = p.ProjectName,
                        ShortDescription = p.Description.Length > 100
                            ? p.Description.Substring(0, 100) + "..."
                            : p.Description,
                        FullDescription = p.Description
                    })
                    .ToList()
            };

            return View(viewModel);
        }


        // Helper method to capitalize first letter
        private static string Capitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }

    }
}
