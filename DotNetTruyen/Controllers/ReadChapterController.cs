using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.Services;
using DotNetTruyen.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotNetTruyen.Controllers
{
    public class ReadChapterController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly UserService _userService;

        public ReadChapterController(DotNetTruyenDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // Get chapter by ID
        public async Task<IActionResult> Index(Guid id)
        {
            // Get the current chapter with its images
            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .Include(c => c.Images.OrderBy(i => i.ImageNumber))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsPublished && c.DeletedAt == null);

            if (chapter == null)
            {
                return NotFound();
            }

			

			// Get previous chapter
			var prevChapter = await _context.Chapters
				.Where(c => c.ComicId == chapter.ComicId &&
					   c.ChapterNumber < chapter.ChapterNumber &&
					   c.IsPublished &&
					   c.DeletedAt == null)
				.OrderByDescending(c => c.ChapterNumber)
				.FirstOrDefaultAsync();

            // Get next chapter
            var nextChapter = await _context.Chapters
                .Where(c => c.ComicId == chapter.ComicId &&
                       c.ChapterNumber > chapter.ChapterNumber &&
                       c.IsPublished &&
                       c.DeletedAt == null)
                .OrderBy(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            // Get all chapters of this comic for the chapter list
            var allChapters = await _context.Chapters
                .Where(c => c.ComicId == chapter.ComicId &&
                       c.IsPublished &&
                       c.DeletedAt == null)
                .OrderByDescending(c => c.ChapterNumber)
                .ToListAsync();

            // Create view model
            var viewModel = new ReadChapterViewModel
            {
                Chapter = chapter,
                PreviousChapter = prevChapter,
                NextChapter = nextChapter,
                AllChapters = allChapters
            };

            return View(viewModel);
        }

        // Read first chapter of a comic
        public async Task<IActionResult> ReadFromBeginning(Guid comicId)
        {
            var firstChapter = await _context.Chapters
                .Where(c => c.ComicId == comicId &&
                       c.IsPublished &&
                       c.DeletedAt == null)
                .OrderBy(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            if (firstChapter == null)
            {
                TempData["ErrorMessage"] = "Không có chương nào được tìm thấy.";
                return RedirectToAction("Index", "Detail", new { id = comicId });
            }

            return RedirectToAction("Index", new { id = firstChapter.Id });
        }
        public async Task<IActionResult> ReadLastChapter(Guid comicId)
        {
            var lastChapter = await _context.Chapters
                .Where(c => c.ComicId == comicId &&
                       c.IsPublished &&
                       c.DeletedAt == null)
                .OrderByDescending(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            if (lastChapter == null)
            {
                TempData["ErrorMessage"] = "Không có chương nào được tìm thấy.";
                return RedirectToAction("Index", "Detail", new { id = comicId });
            }

			return RedirectToAction("Index", new { id = lastChapter.Id });
		}
		public async Task<List<ChapterImage>> GetPublishedChapterImagesAsync()
		{
			return await _context.ChapterImages
				.Where(img => img.Chapter.IsPublished)
				.ToListAsync();
		}

		[HttpPost]
		public async Task<IActionResult> UpdateViewCount(Guid chapterId)
		{
			try
			{
				// Tìm chapter
				var chapter = await _context.Chapters.Include(c => c.Comic).FirstOrDefaultAsync(c => c.Id == chapterId);
				if (chapter != null)
				{
					// Tăng lượt xem
					chapter.Views += 1;
					chapter.Comic.View += 1;
                    

                    
                    // Thêm vào lịch sử đọc của user (nếu user đã đăng nhập)
                    if (User.Identity.IsAuthenticated)
					{
                        await _userService.IncreaseExpAsync(_context, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

						var readHistory = await _context.ReadHistories.FirstOrDefaultAsync(r => r.UserId.ToString() == userId && r.ChapterId == chapterId);
						if (readHistory != null) 
						{
							readHistory.ReadDate = DateTime.Now;
						}
						else
						{
							var readingHistory = new ReadHistory
							{
								UserId = Guid.Parse(userId),
								ChapterId = chapterId,
								ReadDate = DateTime.Now,
								IsRead = true,
							};
							_context.ReadHistories.Add(readingHistory);
						}
					}

					await _context.SaveChangesAsync();
					return Json(new { success = true });
				}
				return Json(new { success = false, message = "Chapter not found" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}
	}
}