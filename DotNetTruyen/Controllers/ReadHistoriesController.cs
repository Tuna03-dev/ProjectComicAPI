using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Data;
using DotNetTruyen.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DotNetTruyen.ViewModels.Management;
using DotNetTruyen.ViewModels;

namespace DotNetTruyen.Controllers
{
    [Authorize]
    public class ReadHistoriesController : Controller
    {
        private readonly DotNetTruyenDbContext _context;

        public ReadHistoriesController(DotNetTruyenDbContext context)
        {
            _context = context;
        }

        // GET: ReadHistories
        [HttpGet("/readHistory")]
        public async Task<IActionResult> Index(string searchQuery = "", int page = 1)
        {
			ViewBag.HistoryTab = "active";
			int pageSize = 9;
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var history = _context.ReadHistories
                .Include(r => r.Chapter)
                .ThenInclude(c => c.Comic)
                .Include(r => r.User)
                .Where(r => r.UserId.ToString() == userId)
                .Join( _context.ReadHistories
                .Where(r => r.UserId.ToString() == userId)
                .GroupBy(r => r.Chapter.ComicId)
                .Select(g => new { ComicId = g.Key, MaxReadDate = g.Max(r => r.ReadDate) }),
                r => new { r.Chapter.ComicId, r.ReadDate },
                g => new { g.ComicId, ReadDate = g.MaxReadDate },
                (r, g) => r)
                .OrderByDescending(r => r!.ReadDate);


            var totalHistories = await history.CountAsync();

			var histories = await history
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var readHistoryViewModel = new ReadHistoryViewModel
			{
				ReadHistories = histories,
				CurrentPage = page,
				TotalPages = (int)Math.Ceiling(totalHistories / (double)pageSize)
			};
			return View("~/Views/User/ReadHistory.cshtml", readHistoryViewModel);
        }


        [HttpGet("/deleteHistory")]
        // GET: ReadHistories/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var readHistory = await _context.ReadHistories
                .Include(r => r.Chapter)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReadHistoryId == id);
            if (readHistory == null)
            {
                return NotFound();
            }
            _context.ReadHistories.Remove(readHistory);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
