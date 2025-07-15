using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.SignalR;
using DotNetTruyen.Hubs;
using DotNetTruyen.ViewModels.Management;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace DotNetTruyen.Controllers.Admin.GenreManagement
{
	[Authorize(Policy = "CanManageGenre")]
	public class GenresController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly ILogger<GenresController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public GenresController(DotNetTruyenDbContext context, IHubContext<NotificationHub> hubContext, ILogger<GenresController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        // GET: Genres
        public async Task<IActionResult> Index(string searchQuery = "", int page = 1)
        {
            int pageSize = 8;

            var genreQuery = _context.Genres.Where(g => g.DeletedAt == null).AsQueryable();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                genreQuery = genreQuery.Where(g => g.GenreName.Contains(searchQuery));
            }

            // Tính toán thống kê
            var totalGenres = await _context.Genres.CountAsync(g => g.DeletedAt == null);
            var totalComics = await _context.Comics.CountAsync(c => c.DeletedAt == null);
            var activeGenres = await _context.Genres
                .Where(g => g.DeletedAt == null && g.ComicGenres.Any(cg => cg.Comic.DeletedAt == null))
                .CountAsync();

            var totalItems = await genreQuery.CountAsync();

            var genres = await genreQuery
                .OrderBy(g => g.GenreName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GenreViewModel
                {
                    Id = g.Id,
                    GenreName = g.GenreName,
                    TotalStories = g.ComicGenres.Count(cg => cg.Comic.DeletedAt == null),
                    UpdatedAt = g.UpdatedAt ?? DateTime.Now 
                })
                .ToListAsync();

            var viewModel = new GenreIndexViewModel
            {
                GenreViewModels = genres,
                SearchQuery = searchQuery,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                TotalGenres = totalGenres,
                TotalComics = totalComics,
                ActiveGenres = activeGenres
            };

            return View("~/Views/Admin/Genres/Index.cshtml", viewModel);
        }

        // GET: Genres/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(m => m.Id == id);
            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }

        // POST: Genres/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGenreViewModel createdGenre)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["ShowModal"] = "true";
                    _logger.LogWarning("Invalid model state.");
                    return View("Index", createdGenre);
                }
                bool isExist = await _context.Genres.AnyAsync(g => g.GenreName == createdGenre.GenreName && g.DeletedAt == null);
                if (isExist)
                {
                    TempData["ErrorMessage"] = "Thể loại này đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                var genre = new Genre
                {
                    GenreName = createdGenre.GenreName
                };

                _context.Add(genre);
                await _context.SaveChangesAsync();

                // Tạo thông báo khi thể loại được tạo thành công
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Thể loại mới",
                    Message = $"Thể loại '{genre.GenreName}' đã được tạo thành công.",
                    Type = "success",
                    Icon = "check-circle",
                    Link = $"/Genres",
                    IsRead = false,
                    
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Gửi thông báo qua SignalR
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    icon = notification.Icon,
                    link = notification.Link
                });

                _logger.LogInformation("Genre created successfully: {GenreName}", genre.GenreName);
                TempData["SuccessMessage"] = "Thể loại đã được tạo thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating genre.");
                ViewData["ErrorMessage"] = "An error occurred while processing your request. Please try again later.";
                return View("Index", createdGenre);
            }
        }

        // GET: Genres/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .Include(g => g.ComicGenres)
                .ThenInclude(cg => cg.Comic)
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (genre == null)
            {
                return NotFound();
            }

            // Prepare ViewModel
            var viewModel = new GenreDetailViewModel
            {
                Id = genre.Id,
                GenreName = genre.GenreName,
                SelectedStoryIds = genre.ComicGenres.Select(cg => cg.ComicId).ToList(),
                Comics = await _context.Comics.ToListAsync() // Get all comics
            };

            return View("~/Views/Admin/Genres/Edit.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SearchStories(string query)
        {
            var comicsQuery = _context.Comics.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                comicsQuery = comicsQuery.Where(c => c.Title.ToLower().Contains(query.ToLower()) && c.DeletedAt == null);
            }

            var comics = await comicsQuery
                .Select(c => new { c.Id, c.Title, c.CoverImage })
                .ToListAsync();

            return Json(comics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditGenreViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var genre = await _context.Genres
                    .Include(g => g.ComicGenres)
                    .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

                if (genre == null)
                {
                    return NotFound();
                }
                bool isExist = await _context.Genres.AnyAsync(g => g.GenreName == model.GenreName && g.Id != id && g.DeletedAt == null);
                if (isExist)
                {
                    TempData["ErrorMessage"] = "Thể loại này đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                genre.GenreName = model.GenreName;
                List<Guid>? selectedStoryIds = string.IsNullOrEmpty(model.SelectedStoryIds)
                    ? new List<Guid>()
                    : JsonSerializer.Deserialize<List<Guid>>(model.SelectedStoryIds);

                // Remove old relationships
                _context.ComicGenres.RemoveRange(genre.ComicGenres);

                // Add new relationships
                foreach (var comicId in selectedStoryIds)
                {
                    var comicGenre = new ComicGenre
                    {
                        GenreId = genre.Id,
                        ComicId = comicId
                    };
                    _context.ComicGenres.Add(comicGenre);
                }

                // Tạo thông báo khi thể loại được cập nhật
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Cập nhật thể loại",
                    Message = $"Thể loại '{model.GenreName}' đã được cập nhật.",
                    Type = "info",
                    Icon = "info-circle",
                    Link = $"/Genres",
                    IsRead = false,
                    
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Gửi thông báo qua SignalR
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    icon = notification.Icon,
                    link = notification.Link
                });

                _logger.LogInformation("Genre updated successfully: {GenreName}", model.GenreName);
                TempData["SuccessMessage"] = "Thể loại đã được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }

            model.Comics = await _context.Comics.ToListAsync();
            return View(model);
        }

        // GET: Genres/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(m => m.Id == id);
            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }

        // POST: Genres/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thể loại để xóa.";
                return RedirectToAction(nameof(Index));
            }

            var hasComics = await _context.ComicGenres
                .AnyAsync(cg => cg.GenreId == id && cg.Comic.DeletedAt == null);

            if (hasComics)
            {
                TempData["ErrorMessage"] = "Không thể xóa thể loại này vì vẫn còn truyện liên kết với nó.";
                return RedirectToAction(nameof(Index));
            }

            genre.DeletedAt = DateTime.UtcNow;
            _context.Update(genre);
            await _context.SaveChangesAsync();

            // Tạo thông báo khi thể loại được xóa
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Xóa thể loại",
                Message = $"Thể loại '{genre.GenreName}' đã được xóa.",
                Type = "warning",
                Icon = "exclamation-circle",
                Link = $"/Genres",
                IsRead = false,
                
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi thông báo qua SignalR
            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                icon = notification.Icon,
                link = notification.Link
            });

            _logger.LogInformation("Genre deleted successfully: {GenreName}", genre.GenreName);
            TempData["SuccessMessage"] = "Thể loại đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool GenreExists(Guid id)
        {
            return _context.Genres.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}