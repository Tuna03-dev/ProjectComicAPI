using DotNetTruyen.Data;
using DotNetTruyen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DotNetTruyen.Controllers
{
    public class FollowsController : Controller
    {
        private readonly DotNetTruyenDbContext _context;

        public FollowsController(DotNetTruyenDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1, int pageSize = 4)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return RedirectToAction("Login", "Account");
            }

          
            var totalComics = _context.Follows
                .Count(f => f.UserId == parsedUserId);

           
            var totalPages = (int)Math.Ceiling((double)totalComics / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages)); 

           
            var followedComics = _context.Follows
                .Where(f => f.UserId == parsedUserId)
                .Include(f => f.Comic)
                .OrderBy(f => f.Comic.Title) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.Comic)
                .ToList();

         
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalComics = totalComics;

            return View(followedComics);
        }
    }
}
