using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.Services;
using DotNetTruyen.ViewModels.Management;
using Microsoft.AspNetCore.Authorization;

namespace DotNetTruyen.Controllers.Admin.Advertisement
{
	[Authorize(Policy = "CanManageUser")]
	public class AdvertisementsController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly IPhoToService _photoService;

        public AdvertisementsController(DotNetTruyenDbContext context, IPhoToService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        // GET: Advertisements
        public async Task<IActionResult> Index()
        {
            var activeAdvertisements = await _context.Advertisements
        .Where(a => a.DeletedAt == null)
        .ToListAsync();

            return View(activeAdvertisements);
        }

        // GET: Advertisements/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var advertisement = await _context.Advertisements
                .FirstOrDefaultAsync(m => m.Id == id);
            if (advertisement == null)
            {
                return NotFound();
            }

            return View(advertisement);
        }

        // GET: Advertisements/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Advertisements/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,LinkTo,ImageUrl,Id,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt,DeletedAt")] AdvertisementsViewModel model)
        {
            

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Error in {state.Key}: {state.Value.Errors[0].ErrorMessage}");
                    }
                }
                
            }

            var advertisement = new DotNetTruyen.Models.Advertisement
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                LinkTo = model.LinkTo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
               
            };

            if (model.ImageUrl == null)
            {
                Console.WriteLine("CoverImage is null");
            }

            
            if (model.ImageUrl != null)
            {
                var uploadResult = await _photoService.AddPhotoAsync(model.ImageUrl);
                if (uploadResult != null)
                {
                    advertisement.ImageUrl = uploadResult;
                    Console.WriteLine("Image uploaded successfully: " );
                }
                else
                {
                    Console.WriteLine("Image upload failed");
                }
            }

      

            _context.Add(advertisement);
            await _context.SaveChangesAsync();

           
            return RedirectToAction(nameof(Index));
        }

        // GET: Advertisements/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var advertisenments = await _context.Advertisements.FindAsync(id);


            if (advertisenments == null)
            {
                return NotFound();
            }

            var viewModel = new AdvertisementsViewModel
            {
                Id = advertisenments.Id,
                Title = advertisenments.Title,
                ImageUrlPath = advertisenments.ImageUrl,
                LinkTo = advertisenments.LinkTo

            };

            return View(viewModel);
        }

        // POST: Advertisements/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Title,LinkTo,ImageUrl,ImageUrlPath,Id,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt,DeletedAt")] AdvertisementsViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var advertisement = await _context.Advertisements.FindAsync(id);
            if (advertisement == null)
            {
                return NotFound();
            }

            
            advertisement.Title = model.Title;
            advertisement.LinkTo = model.LinkTo;
            advertisement.UpdatedAt = DateTime.UtcNow;

            
            if (model.ImageUrl != null && model.ImageUrl.Length > 0)
            {
                var uploadResult = await _photoService.AddPhotoAsync(model.ImageUrl);
                if (uploadResult != null)
                {
                    advertisement.ImageUrl = uploadResult; 
                    Console.WriteLine("Image uploaded successfully: " + uploadResult);
                }
                else
                {
                    Console.WriteLine("Image upload failed");
                }
            }
            else
            {
                
                advertisement.ImageUrl = model.ImageUrlPath;  
            }

            _context.Update(advertisement);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Advertisements/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var advertisements = await _context.Advertisements
                .FirstOrDefaultAsync(m => m.Id == id);
            if (advertisements == null)
            {
                return NotFound();
            }

            return View(advertisements);
        }

        // POST: Advertisements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var advertisements = await _context.Advertisements.FindAsync(id);

            if (advertisements == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy quảng cáo để xóa.";
                return RedirectToAction(nameof(Index));
            }
            advertisements.DeletedAt = DateTime.UtcNow;
            _context.Update(advertisements);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool AdvertisementExists(Guid id)
        {
            return _context.Advertisements.Any(e => e.Id == id);
        }
    }
}