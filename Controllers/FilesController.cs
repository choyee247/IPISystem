using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;

public class FilesController : Controller
{
    private readonly PMSDbContext _context;
    private readonly IWebHostEnvironment _env;

    public FilesController(PMSDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public IActionResult Download(int id)
    {
        var file = _context.ProjectFiles.FirstOrDefault(f => f.ProjectFilePkId == id);

        if (file == null)
        {
            return NotFound();
        }

        // ⭐ folder choose based on FileType
        var folder = file.FileType == "Cancel" ? "cancel" : "projects";

        var path = Path.Combine(_env.WebRootPath, "uploads", folder, file.FilePath);

        if (!System.IO.File.Exists(path))
        {
            return NotFound("File not found: " + path);
        }

        var bytes = System.IO.File.ReadAllBytes(path);

        return File(bytes,
            "application/octet-stream", // ⭐ better generic type
            file.FilePath);
    }
}