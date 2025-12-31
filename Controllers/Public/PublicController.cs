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

namespace ProjectManagementSystem.Controllers.Public
{
    public class PublicController : Controller
    {
        private readonly PMSDbContext _context;
        private const string AccessCode = "OLDSTUDENT2025"; // your secret code
        private readonly string _imageFolder = "uploads/successstories";
        public PublicController(PMSDbContext context)
        {
            _context = context;
        }      
        public async Task<IActionResult> ProjectIdeas(int? projectTypeId, int? languageId, string? searchTerm, int page = 1)
        {
            var projectsQuery = _context.Projects
                .Include(p => p.ProjectTypePk)
                .Include(p => p.LanguagePk)
                .Include(p => p.ProjectFiles)
                .Where(p => (bool)!p.IsDeleted && p.Status == "Approved");

            if (projectTypeId.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.ProjectTypePkId == projectTypeId);
            }

            if (languageId.HasValue)
            {
                projectsQuery = projectsQuery.Where(p => p.LanguagePkId == languageId);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectName.Contains(searchTerm) ||
                    p.Description.Contains(searchTerm));
            }

            var totalProjects = await projectsQuery.CountAsync();
            var pageSize = 6;

            var paginatedProjects = await projectsQuery
                .OrderByDescending(p => p.ProjectPkId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProjectIdeasViewModel
            {
                Projects = paginatedProjects,
                ProjectTypes = await _context.ProjectTypes
                    .Select(pt => new SelectListItem
                    {
                        Value = pt.ProjectTypePkId.ToString(),
                        Text = pt.TypeName
                    }).ToListAsync(),

                Languages = projectTypeId.HasValue
                    ? await _context.Languages
                        .Where(l => l.ProjectTypePkId == projectTypeId)
                        .Select(l => new SelectListItem
                        {
                            Value = l.LanguagePkId.ToString(),
                            Text = l.LanguageName
                        }).ToListAsync()
                    : new List<SelectListItem>(),

                SelectedProjectTypeId = projectTypeId,
                SelectedLanguageId = languageId,
                SearchTerm = searchTerm,
                TotalProjects = totalProjects,
                CurrentPage = page
            };

            return View(viewModel);
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
        public async Task<IActionResult> InternshipCompanies(int? selectedCityId, int page = 1)
        {
            int pageSize = 5;

            // All cities that have at least one internship company
            var allCities = await _context.Cities
                .Where(c => _context.Companies.Any(co => co.CityPkId == c.CityPkId))
                .OrderBy(c => c.CityName)
                .ToListAsync();

            // Filter for display (if selected city is chosen)
            var filteredCities = selectedCityId.HasValue
                ? allCities.Where(c => c.CityPkId == selectedCityId.Value).ToList()
                : allCities;

            var pagedCities = filteredCities.ToPagedList(page, pageSize);

            var viewModel = new CityListViewModel
            {
                SelectedCityId = selectedCityId,
                CityList = allCities.Select(c => new SelectListItem
                {
                    Value = c.CityPkId.ToString(),
                    Text = c.CityName
                }),
                TotalCities = filteredCities.Count,
                Cities = (IPagedList<DBModels.City>)pagedCities,
                CurrentPage = page,
                //TotalPages = pagedCities.PageCount
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
