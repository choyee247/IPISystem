using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;

namespace ProjectManagementSystem.Controllers
{
    public class ProjectTypeController : BaseStudentController
    {
        private readonly PMSDbContext _context;

        public ProjectTypeController(PMSDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var projectType = _context.ProjectTypes.ToList();
            return View(projectType);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var projectType = await _context.ProjectTypes
                .FirstOrDefaultAsync(m => m.ProjectTypePkId == id);
            if (projectType == null) return NotFound();

            return View(projectType);
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TypeName")] DBModels.ProjectType projectType)
        {
            //if (ModelState.IsValid)
            //{
                _context.Add(projectType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}
            //return View(projectType);
        }

        public IActionResult Edit(int id)
        {
            var projectType = _context.ProjectTypes.Find(id);
            if (projectType == null)
            {
                return RedirectToAction("Index", "ProjectType");
            }

            return View(projectType);
        }

        [HttpPost]
        public IActionResult Edit(int id, DBModels.ProjectType projectType)
        {
            var ptype = _context.ProjectTypes.Find(id);

            if (ptype == null)
            {
                return NotFound();
            }

            ptype.TypeName = projectType.TypeName;
            _context.SaveChanges();
            return RedirectToAction("Index", "ProjectType");
        }
    }

}

