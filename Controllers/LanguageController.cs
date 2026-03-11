using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using X.PagedList;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjectManagementSystem.Controllers
{
    public class LanguageController : BaseStudentController
    {
        private readonly PMSDbContext _context;

        public LanguageController(PMSDbContext context) : base(context)
        {
            _context = context;
        }

        // GET: Language
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 8;               // Number of items per page
            int pageNumber = page ?? 1;     // Current page number (default 1)

            var languages = await _context.Languages
                .Include(l => l.ProjectTypePk)
                                .OrderBy(l => l.LanguagePkId)
                                .ToPagedListAsync(pageNumber, pageSize);

            return View(languages);
        }

        // GET: Language/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var language = await _context.Languages
                .FirstOrDefaultAsync(m => m.LanguagePkId == id);

            if (language == null)
                return NotFound();

            return View(language);
        }

        // GET: Language/Create
        public IActionResult Create()
        {
            ViewBag.ProjectTypes = new SelectList(_context.ProjectTypes.OrderBy(t => t.TypeName), "ProjectTypePkId", "TypeName");
            return View();
        }

        // POST: Language/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DBModels.Language language)
        {
            if (!ModelState.IsValid) // Fix: Validate properly
            {
                _context.Languages.Add(language);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Reload ProjectTypes if validation fails
            ViewBag.ProjectTypes = new SelectList(_context.ProjectTypes.OrderBy(t => t.TypeName), "ProjectTypePkId", "TypeName", language.ProjectTypePkId);
            return View(language);
        }

        // GET: Language/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var language = await _context.Languages.FindAsync(id);
            if (language == null) return NotFound();

            ViewBag.ProjectTypes = new SelectList(_context.ProjectTypes.OrderBy(t => t.TypeName), "ProjectTypePkId", "TypeName", language.ProjectTypePkId);
            return View(language);
        }

        // POST: Language/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DBModels.Language language)
        {
            if (id != language.LanguagePkId)
                return NotFound();

            if (ModelState.IsValid) // Fix: run update only if valid
            {
                try
                {
                    _context.Update(language);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Languages.Any(e => e.LanguagePkId == language.LanguagePkId))
                        return NotFound();
                    else
                        throw;
                }
            }

            // Reload ProjectTypes dropdown if validation fails
            ViewBag.ProjectTypes = new SelectList(_context.ProjectTypes.OrderBy(t => t.TypeName), "ProjectTypePkId", "TypeName", language.ProjectTypePkId);
            return View(language);
        }

        // GET: Language/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var language = await _context.Languages.FindAsync(id);
            if (language == null) return NotFound();

            return View(language);
        }

        // POST: Language/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var language = await _context.Languages.FindAsync(id);
            if (language != null)
            {
                _context.Languages.Remove(language);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}