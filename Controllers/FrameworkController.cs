using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

public class FrameworkController : BaseStudentController
{
    private readonly PMSDbContext _context;

    public FrameworkController(PMSDbContext context) : base(context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index(int? page)
    {
        var rollNumber = HttpContext.Session.GetString("RollNumber");
        if (string.IsNullOrEmpty(rollNumber))
        {
            return RedirectToAction("Login", "StudentLogin");
        }

        int pageSize = 3;
        int pageNumber = page ?? 1;

        var frameworks = await _context.Frameworks
            .Include(f => f.LanguagePk)
            .OrderBy(f => f.FrameworkPkId)
            .ToPagedListAsync(pageNumber, pageSize);

        return View(frameworks);
    }

    // GET: Framework/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var framework = await _context.Frameworks
            .Include(f => f.LanguagePk) // Include the related Language
            .FirstOrDefaultAsync(f => f.FrameworkPkId == id);

        if (framework == null) return NotFound();

        return View(framework);
    }

    // GET: Framework/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var languages = await _context.Languages
            .Select(l => new SelectListItem
            {
                Value = l.LanguagePkId.ToString(),
                Text = l.LanguageName
            })
            .ToListAsync();

        var model = new FrameworkCreateViewModel
        {
            Languages = languages
        };

        return View(model);
    }

    // POST: Framework/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FrameworkCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Reload Languages if validation fails
            model.Languages = await _context.Languages
                .Select(l => new SelectListItem
                {
                    Value = l.LanguagePkId.ToString(),
                    Text = l.LanguageName
                })
                .ToListAsync();
            return View(model);
        }

        var framework = new ProjectManagementSystem.DBModels.Framework
        {
            FrameworkName = model.FrameworkName,
            LanguagePkId = model.SelectedLanguageId
        };

        _context.Frameworks.Add(framework);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index)); // Make sure you have an Index action or redirect elsewhere
    }
    // GET: Framework/Edit/5
    // GET: Framework/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var framework = await _context.Frameworks.FindAsync(id);
        if (framework == null) return NotFound();

        var viewModel = new FrameworkCreateViewModel
        {
            FrameworkName = framework.FrameworkName,
            SelectedLanguageId = framework.LanguagePkId,
            Languages = await _context.Languages
                .Select(l => new SelectListItem
                {
                    Value = l.LanguagePkId.ToString(),
                    Text = l.LanguageName
                }).ToListAsync()
        };

        ViewBag.FrameworkId = framework.FrameworkPkId;
        return View(viewModel);
    }

    // POST: Framework/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FrameworkCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Reload dropdown if validation fails
            model.Languages = await _context.Languages
                .Select(l => new SelectListItem
                {
                    Value = l.LanguagePkId.ToString(),
                    Text = l.LanguageName
                }).ToListAsync();

            ViewBag.FrameworkId = id;
            return View(model);
        }

        var framework = await _context.Frameworks.FindAsync(id);
        if (framework == null) return NotFound();

        framework.FrameworkName = model.FrameworkName;
        framework.LanguagePkId = model.SelectedLanguageId;

        _context.Update(framework);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var framework = await _context.Frameworks
            .Include(f => f.LanguagePk)
            .FirstOrDefaultAsync(f => f.FrameworkPkId == id);

        if (framework == null) return NotFound();

        return View(framework);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var framework = await _context.Frameworks.FindAsync(id);
        if (framework == null)
            return NotFound();

        _context.Frameworks.Remove(framework);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }



    // Optional: Index action to list frameworks

}
