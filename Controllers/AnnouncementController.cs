using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace ProjectManagementSystem.Controllers
{
    //[Authorize(Roles = "Admin,Teacher")] // Teacher + Admin only
    public class AnnouncementController : Controller
    {
        private readonly PMSDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AnnouncementController(PMSDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Announcement
        [AllowAnonymous]
        public IActionResult Index(int? page)
        {
            int pageSize = 7;
            int pageNumber = page ?? 1;

            var announcements = _context.Announcements
                .OrderByDescending(a => a.CreatedDate)
                .ToPagedList(pageNumber, pageSize);

            return View(announcements);
        }

        // GET: /Announcement/Create
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new AnnouncementViewModel();
            return View();
        }

        // POST: /Announcement/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Announcement model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Handle file upload
        //        if (model.Attachment != null)
        //        {
        //            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/announcements");
        //            if (!Directory.Exists(uploadsFolder))
        //                Directory.CreateDirectory(uploadsFolder);

        //            string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(model.Attachment.FileName);
        //            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //            using (var stream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await model.Attachment.CopyToAsync(stream);
        //            }

        //            model.FilePath = "/uploads/announcements/" + uniqueFileName;
        //        }

        //        model.CreatedDate = DateTime.Now;
        //        _context.Announcements.Add(model);
        //        await _context.SaveChangesAsync();

        //        TempData["Success"] = "Announcement created successfully!";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(model);
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[AllowAnonymous]
        //public async Task<IActionResult> Create(AnnouncementViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (model.Attachment != null)
        //        {
        //            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/announcements");
        //            if (!Directory.Exists(uploadsFolder))
        //                Directory.CreateDirectory(uploadsFolder);

        //            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Attachment.FileName);
        //            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //            using (var stream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await model.Attachment.CopyToAsync(stream);
        //            }

        //            model.FilePath = "/uploads/announcements/" + uniqueFileName;
        //        }

        //        model.CreatedDate = DateTime.Now;
        //        _context.Add(model);
        //        await _context.SaveChangesAsync();


        //        var allStudents = await _context.Students.ToListAsync();
        //        foreach (var student in allStudents)
        //        {
        //            var notification = new DBModels.Notification
        //            {
        //                UserId = student.StudentPkId,
        //                Title = "New Announcement",
        //                Message = model.Title, // Short message
        //                NotificationType = "Announcement",
        //                CreatedAt = DateTime.Now,
        //               // ProjectPkId = null // If announcement is related to project, link it here
        //            };
        //            _context.Notifications.Add(notification);
        //        }
        //        await _context.SaveChangesAsync();
        //        // ===========================

        //        TempData["Success"] = "Announcement created successfully!";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(model);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create(AnnouncementViewModel model)
        {
            if (ModelState.IsValid)
            {
                // File upload
                string filePath = null;
                if (model.Attachment != null)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/announcements");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Attachment.FileName);
                    filePath = "/uploads/announcements/" + uniqueFileName;
                    string fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(stream);
                    }
                }

                // ViewModel → Entity mapping
                var entity = new Announcement
                {
                    Title = model.Title,
                    Message = model.Message,
                    StartDate = model.StartDate,
                    ExpiryDate = model.ExpiryDate,
                    BlocksSubmissions = model.BlocksSubmissions,
                    IsActive = model.IsActive,
                    FilePath = filePath,
                    CreatedDate = DateTime.Now
                };

                // DB save
                _context.Announcements.Add(entity);
                await _context.SaveChangesAsync();

                // Notifications
                //var allStudents = await _context.Students.ToListAsync();
                var allStudents = await _context.Students
                .Include(s => s.ProjectMembers)
                .ThenInclude(pm => pm.ProjectPk)
                .ToListAsync();
                foreach (var student in allStudents)
                {
                    var projectId = student.ProjectMembers
                                           .Select(pm => pm.ProjectPkId)
                                           .FirstOrDefault();

                    if (projectId == 0 || projectId == null)
                    {
                        continue; 
                    }

                    var notification = new Notification
                    {
                        UserId = student.StudentPkId,
                        Title = "New Announcement",
                        Message = entity.Title,
                        NotificationType = "Announcement",
                        CreatedAt = DateTime.Now,
                        ProjectPkId = projectId,
                        IsRead=false,
                        IsDeleted=false
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = "Announcement created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            var vm = new AnnouncementViewModel
            {
                AnnouncementId = announcement.AnnouncementId,
                Title = announcement.Title,
                BlocksSubmissions = (bool)announcement.BlocksSubmissions,
                IsActive = (bool)announcement.IsActive,
                Message = announcement.Message,
                FilePath = announcement.FilePath
            };

            return View(vm); // MUST return ViewModel
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnouncementViewModel model)
        {
            if (id != model.AnnouncementId) return NotFound();

            if (ModelState.IsValid)
                return View(model);

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            try
            {
                // File upload
                if (model.Attachment != null)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/announcements");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(model.Attachment.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(stream);
                    }

                    // delete old file
                    if (!string.IsNullOrEmpty(announcement.FilePath))
                    {
                        string oldFile = Path.Combine(_env.WebRootPath,
                            announcement.FilePath.TrimStart('/').Replace("/", "\\"));

                        if (System.IO.File.Exists(oldFile))
                            System.IO.File.Delete(oldFile);
                    }

                    announcement.FilePath = "/uploads/announcements/" + uniqueFileName;
                }

                // Update fields
                announcement.Title = model.Title;
                announcement.Message = model.Message;
                announcement.BlocksSubmissions = model.BlocksSubmissions; 
                announcement.IsActive = model.IsActive;

                _context.Update(announcement);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Announcement updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating announcement: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: /Announcement/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var announcement = await _context.Announcements.FindAsync(id);
        //    if (announcement == null) return NotFound();

        //    return View(announcement);
        //}

        //// POST: /Announcement/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, AnnouncementViewModel model)
        //{
        //    if (id != model.AnnouncementId) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            // Handle new file upload
        //            if (model.Attachment != null)
        //            {
        //                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/announcements");
        //                if (!Directory.Exists(uploadsFolder))
        //                    Directory.CreateDirectory(uploadsFolder);

        //                string uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(model.Attachment.FileName);
        //                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //                using (var stream = new FileStream(filePath, FileMode.Create))
        //                {
        //                    await model.Attachment.CopyToAsync(stream);
        //                }

        //                // Delete old file if exists
        //                if (!string.IsNullOrEmpty(model.FilePath))
        //                {
        //                    string oldFile = Path.Combine(_env.WebRootPath, model.FilePath.TrimStart('/').Replace("/", "\\"));
        //                    if (System.IO.File.Exists(oldFile))
        //                        System.IO.File.Delete(oldFile);
        //                }

        //                model.FilePath = "/uploads/announcements/" + uniqueFileName;
        //            }

        //            _context.Update(model);
        //            await _context.SaveChangesAsync();
        //            TempData["Success"] = "Announcement updated successfully!";
        //        }
        //        catch (Exception ex)
        //        {
        //            TempData["Error"] = "Error updating announcement: " + ex.Message;
        //        }

        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(model);
        //}

        // GET: /Announcement/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // POST: /Announcement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                // Delete attached file if exists
                if (!string.IsNullOrEmpty(announcement.FilePath))
                {
                    string file = Path.Combine(_env.WebRootPath, announcement.FilePath.TrimStart('/').Replace("/", "\\"));
                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                }

                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Announcement deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Announcement/Detail/5
        [AllowAnonymous]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
                return NotFound();

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementId == id);

            if (announcement == null)
                return NotFound();

            return View(announcement);
        }

        //// Student view: only active announcements
        //[AllowAnonymous]
        //public IActionResult StudentView()
        //{
        //    var activeAnnouncements = _context.Announcements
        //        .AsEnumerable() // important: bring to memory to use IsActive property
        //        .Where(a => a.IsActive == false)
        //        .OrderByDescending(a => a.StartDate)
        //        .ToList();

        //    return View(activeAnnouncements);
        //}
        [AllowAnonymous]
        public IActionResult StudentView()
        {
            var activeAnnouncements = _context.Announcements
                .Where(a => a.IsActive == true) 
                .OrderByDescending(a => a.CreatedDate)
                .Select(a => new AnnouncementViewModel
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Message = a.Message,           
                    CreatedDate = a.CreatedDate,
                    StartDate = a.StartDate,
                    ExpiryDate = a.ExpiryDate,
                    BlocksSubmissions = (bool)a.BlocksSubmissions,
                    FilePath = a.FilePath,
                    AdminActivityLogId = a.AdminActivityLogId
                })
                .ToList();

            return View(activeAnnouncements);
        }
    }
}
