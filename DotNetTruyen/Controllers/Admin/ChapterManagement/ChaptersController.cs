using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.ViewModels.Management;
using DotNetTruyen.Services;
using NuGet.Packaging;
using Microsoft.AspNetCore.SignalR;
using DotNetTruyen.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace DotNetTruyen.Controllers.Admin.ChapterManagement
{
	[Authorize(Policy = "CanManageChapter")]
	public class ChaptersController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly IPhoToService _imageUploadService;
        private readonly ILogger<ChaptersController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationService _notificationService;

        public ChaptersController(DotNetTruyenDbContext context, IPhoToService imageUploadService, ILogger<ChaptersController> logger, IHubContext<NotificationHub> hubContext, INotificationService notificationService)
        {
            _context = context;
            _imageUploadService = imageUploadService;
            _logger = logger;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }







        // GET: Chapters
        public async Task<IActionResult> Index(Guid? comicId, string searchQuery, int page = 1, int pageSize = 10)
        {
            if (comicId == null) return NotFound();

            
            var comicExists = await _context.Comics.AnyAsync(c => c.Id == comicId && c.DeletedAt == null);
            if (!comicExists)
            {
                return NotFound();
            }

            
            var query = _context.Chapters
                .Where(c => c.ComicId == comicId && c.DeletedAt == null)
                .OrderBy(c => c.ChapterNumber)
                .Select(c => new ChapterViewModel
                {
                    Id = c.Id,
                    ChapterTitle = c.ChapterTitle,
                    ChapterNumber = c.ChapterNumber,
                    PublishedDate = c.PublishedDate,
                    IsPublished = c.IsPublished,
                    Views = c.Views,
                    ComicId = c.ComicId
                })
                .AsQueryable();


            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.Trim().ToLower();
                
                if (int.TryParse(searchQuery, out int chapterNumber))
                {
                    query = query.Where(c => c.ChapterTitle.ToLower().Contains(searchQuery) || c.ChapterNumber == chapterNumber);
                }
                else
                {
                    query = query.Where(c => c.ChapterTitle.ToLower().Contains(searchQuery));
                }
            }


            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var chapters = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            
            var viewModel = new ChapterIndexViewModel
            {
                ChapterViewModels = chapters,
                SearchQuery = searchQuery,
                CurrentPage = page,
                TotalPages = totalPages,
                ComicId = comicId.Value
            };

            return View("~/Views/Admin/Chapters/Index.cshtml", viewModel);
        }

        // GET: Chapters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(m => m.ChapterNumber == id);
            if (chapter == null)
            {
                return NotFound();
            }

            return View(chapter);
        }

        // GET: Chapters/Create
        public IActionResult Create(Guid comicId)
        {
            var model = new CreateChapterViewModel { ComicId = comicId };
            return View("~/Views/Admin/Chapters/Create.cshtml",model);
        }

        // POST: Chapters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateChapterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/Chapters/Create.cshtml", model);
            }

            var comicExists = await _context.Comics.AnyAsync(c => c.Id == model.ComicId && c.DeletedAt == null);
            if (!comicExists)
            {
                TempData["ErrorMessage"] = "Truyện không tồn tại hoặc đã bị xóa.";
                return View("~/Views/Admin/Chapters/Create.cshtml", model);
            }

            var chapterExists = await _context.Chapters
                .AnyAsync(c => c.ComicId == model.ComicId
                            && c.ChapterNumber == model.ChapterNumber
                            && c.DeletedAt == null);
            if (chapterExists)
            {
                TempData["ErrorMessage"] = $"Chapter số {model.ChapterNumber} đã tồn tại trong truyện này. Vui lòng chọn số chapter khác.";
                return View("~/Views/Admin/Chapters/Create.cshtml", model);
            }

            var chapter = new Chapter
            {
                Id = Guid.NewGuid(),
                ChapterTitle = model.ChapterTitle,
                ChapterNumber = model.ChapterNumber,
                PublishedDate = model.PublishedDate.HasValue ? model.PublishedDate.Value.ToUniversalTime() : DateTime.UtcNow,
                Views = 0,
                IsPublished = model.PublishedDate.HasValue ? true : false,
                ComicId = model.ComicId
            };

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();

            if (model.Images != null && model.Images.Count > 0)
            {
                var imageUrls = await _imageUploadService.AddListPhotoAsync(model.Images);
                var orders = !string.IsNullOrEmpty(model.ImageOrders)
                    ? model.ImageOrders.Split(',').Select(int.Parse).ToList()
                    : Enumerable.Range(0, imageUrls.Count).ToList();

                var chapterImages = new List<ChapterImage>();
                for (int i = 0; i < imageUrls.Count; i++)
                {
                    chapterImages.Add(new ChapterImage
                    {
                        Id = Guid.NewGuid(),
                        ChapterId = chapter.Id,
                        ImageUrl = imageUrls[i],
                        ImageNumber = orders[i]
                    });
                }

                _context.ChapterImages.AddRange(chapterImages);
                await _context.SaveChangesAsync();
            }

            var comic = await _context.Comics.FirstOrDefaultAsync(c => c.Id == model.ComicId);
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Tạo chương mới",
                Message = $"Chương '{chapter.ChapterTitle}' của truyện '{comic.Title}' đã được tạo.",
                Type = "info",
                Icon = "info-circle",
                Link = $"/Chapters/Edit/{chapter.Id}",
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                icon = notification.Icon,
                link = notification.Link
            });

 
            //if (chapter.IsPublished)
            //{

            //    _ = Task.Factory.StartNew(async () =>
            //    {
            //        await _notificationService.SendFollowerNotificationsAsync(
            //            chapter.Id,
            //            model.ComicId,
            //            chapter.ChapterTitle,
            //            comic.Title);
            //    }, TaskCreationOptions.LongRunning).Unwrap();
            //}

            TempData["SuccessMessage"] = "Chương đã được tạo thành công!";
            return RedirectToAction("Index", new { comicId = model.ComicId });
        }

        // GET: Chapters/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null)
            {
                return NotFound();
            }

            var viewModel = new EditChapterViewModel
            {
                Id = chapter.Id,
                ComicId = chapter.ComicId,
                ChapterTitle = chapter.ChapterTitle,
                ChapterNumber = chapter.ChapterNumber,
                PublishedDate = chapter.PublishedDate,
                ExistingImages = chapter.Images?
                    .Where(i => i.DeletedAt == null)
                    .Select(i => new ChapterImageViewModel
                    {
                        ImageNumber = i.ImageNumber,
                        ImageUrl = i.ImageUrl
                    })
                    .OrderBy(i => i.ImageNumber)
                    .ToList() ?? new List<ChapterImageViewModel>()
            };

            return View("~/Views/Admin/Chapters/Edit.cshtml", viewModel);
        }

        // POST: Chapters/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditChapterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await ReturnViewWithImages(model);
            }

            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Images)
                    .FirstOrDefaultAsync(c => c.Id == model.Id && c.DeletedAt == null);

                if (chapter == null)
                {
                    return NotFound();
                }
                
                var comicExists = await _context.Comics.AnyAsync(c => c.Id == model.ComicId && c.DeletedAt == null);
                if (!comicExists)
                {
                    TempData["ErrorMessage"] = "Truyện không tồn tại hoặc đã bị xóa.";
                    return await ReturnViewWithImages(model);
                }

                var chapterNumberExists = await _context.Chapters
                    .AnyAsync(c => c.ComicId == model.ComicId
                        && c.ChapterNumber == model.ChapterNumber
                        && c.Id != model.Id
                        && c.DeletedAt == null);
                if (chapterNumberExists)
                {
                    TempData["ErrorMessage"] = $"Chapter số {model.ChapterNumber} đã tồn tại trong truyện này.";
                    return await ReturnViewWithImages(model);
                }

                // Update chapter properties
                chapter.ChapterTitle = model.ChapterTitle;
                chapter.ChapterNumber = model.ChapterNumber;
                chapter.PublishedDate = model.PublishedDate.HasValue ? model.PublishedDate.Value.ToUniversalTime() : DateTime.Now;
                chapter.IsPublished = model.PublishedDate.HasValue;
                

                var existingImages = chapter.Images?.Where(i => i.DeletedAt == null).ToList();
                var imageOrders = !string.IsNullOrEmpty(Request.Form["ImageOrders"])
                    ? Request.Form["ImageOrders"].ToString().Split(',').Select(int.Parse).ToList()
                    : null;

                if (model.Images != null && model.Images.Count > 0)
                {
                    // Case 1: New images uploaded - soft delete old images and add new ones
                    if (existingImages != null && existingImages.Any())
                    {
                        foreach (var image in existingImages)
                        {
                            image.DeletedAt = DateTime.Now;
                        }
                        _context.ChapterImages.UpdateRange(existingImages); // Update soft delete for old images
                    }

                    var imageUrls = await _imageUploadService.AddListPhotoAsync(model.Images);
                    var orders = imageOrders ?? Enumerable.Range(0, imageUrls.Count).ToList();

                    var newChapterImages = new List<ChapterImage>();
                    for (int i = 0; i < imageUrls.Count; i++)
                    {
                        newChapterImages.Add(new ChapterImage
                        {
                            Id = Guid.NewGuid(),
                            ChapterId = chapter.Id,
                            ImageUrl = imageUrls[i],
                            ImageNumber = orders[i],
                            
                        });
                    }

                    _context.ChapterImages.AddRange(newChapterImages);
                }
                else if (imageOrders != null && existingImages != null && existingImages.Any())
                {
                    // Case 2: No new images uploaded, update order of existing images
                    if (imageOrders.Count != existingImages.Count)
                    {
                        _logger.LogWarning("Mismatch between number of image orders ({OrderCount}) and existing images ({ImageCount}) for chapter {ChapterId}",
                            imageOrders.Count, existingImages.Count, chapter.Id);
                        TempData["ErrorMessage"] = "Lỗi: Số lượng thứ tự ảnh không khớp với số ảnh hiện có.";
                        return await ReturnViewWithImages(model);
                    }

                    var orderedImages = existingImages.OrderBy(i => i.ImageNumber).ToList();
                    for (int i = 0; i < orderedImages.Count; i++)
                    {
                        orderedImages[i].ImageNumber = imageOrders[i];

                    }
                    _context.ChapterImages.UpdateRange(orderedImages);
                }

                var comic = await _context.Comics.FirstOrDefaultAsync(c => c.Id == model.ComicId);
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Cập nhật chương",
                    Message = $"Chương '{chapter.ChapterTitle}' của truyện '{comic.Title}' đã được cập nhật.",
                    Type = "info",
                    Icon = "info-circle",
                    Link = $"/Chapters/Edit/{chapter.Id}",
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    icon = notification.Icon,
                    link = notification.Link
                });

                TempData["SuccessMessage"] = "Chapter đã được cập nhật thành công!";
                return RedirectToAction("Index", new { comicId = model.ComicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chapter {ChapterId}", model.Id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật chapter. Vui lòng thử lại.";
                return await ReturnViewWithImages(model);
            }
        }

        private async Task<IActionResult> ReturnViewWithImages(EditChapterViewModel model)
        {
            model.ExistingImages = await _context.Chapters
                .Where(c => c.Id == model.Id)
                .SelectMany(c => c.Images)
                .Where(i => i.DeletedAt == null)
                .Select(i => new ChapterImageViewModel
                {
                    ImageUrl = i.ImageUrl,
                    ImageNumber = i.ImageNumber
                })
                .OrderBy(i => i.ImageNumber)
                .ToListAsync();
            return View("~/Views/Admin/Chapters/Edit.cshtml", model);
        }


        // GET: Chapters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(m => m.ChapterNumber == id);
            if (chapter == null)
            {
                return NotFound();
            }

            return View(chapter);
        }

        // POST: Chapters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Comic) // Include Comic để lấy thông tin cho notification
                .FirstOrDefaultAsync(c => c.ChapterNumber == id && c.DeletedAt == null);

            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Chapter không tồn tại hoặc đã bị xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Soft delete chapter
            chapter.DeletedAt = DateTime.Now;
            _context.Chapters.Update(chapter);

            // Thêm notification khi xóa mềm thành công
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Xóa chương",
                Message = $"Chương '{chapter.ChapterTitle}' của truyện '{chapter.Comic.Title}' đã bị xóa.",
                Type = "warning",
                Icon = "trash",
                Link = $"/Chapters?comicId={chapter.ComicId}", 
                IsRead = false
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                icon = notification.Icon,
                link = notification.Link
            });

            TempData["SuccessMessage"] = "Chapter đã được xóa thành công!";
            return RedirectToAction(nameof(Index), new { comicId = chapter.ComicId });
        }

        private bool ChapterExists(int id)
        {
            return _context.Chapters.Any(e => e.ChapterNumber == id && e.DeletedAt == null);
        }
    }
}
