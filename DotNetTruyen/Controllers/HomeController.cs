    using DotNetTruyen.Data;
    using DotNetTruyen.Models;
    using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

    namespace DotNetTruyen.Controllers
    {
        public class HomeController : Controller
        {
            private readonly ILogger<HomeController> _logger;
            private readonly DotNetTruyenDbContext _context;

            public HomeController(ILogger<HomeController> logger,DotNetTruyenDbContext context)
            {
                _logger = logger;
                _context = context;
            }

            public IActionResult Index()
            {
                var comics = _context.Comics.Where(c => c.DeletedAt == null).Include(c => c.Chapters).ToList();
            var advertisements = _context.Advertisements
                  .Where(a => (a.Title == "left" || a.Title == "right" || a.Title == "top" || a.Title == "bot") && a.DeletedAt == null)
               .ToList();

            ViewBag.Advertisements = advertisements;
            return View(comics);
            }

            public IActionResult Privacy()
            {
                return View();
            }

            [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            public IActionResult Error()
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }
    }
