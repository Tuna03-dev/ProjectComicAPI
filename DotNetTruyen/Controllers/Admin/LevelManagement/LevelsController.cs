using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.ViewModels.Management;
using Microsoft.AspNetCore.SignalR;
using DotNetTruyen.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace DotNetTruyen.Controllers.Admin.LevelManagement
{
	[Authorize(Policy = "CanManageRank")]
	public class LevelsController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        

        public LevelsController(DotNetTruyenDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;

        }

        private async Task EnsureDefaultLevelExistsAsync()
        {
            if (!await _context.Levels.AnyAsync(l => l.LevelNumber == 0 && l.DeletedAt == null))
            {
                var defaultLevel = new Level
                {
                    Id = Guid.NewGuid(),
                    LevelNumber = 0,
                    Name = "Level 0",
                    ExpRequired = 0,
                    UpdatedAt = DateTime.Now
                };

                _context.Levels.Add(defaultLevel);
                await _context.SaveChangesAsync();
            }
        }

        // GET: Levels/Index
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchQuery = null)
        {
            await EnsureDefaultLevelExistsAsync();

            var query = _context.Levels.Where(l => l.DeletedAt == null).AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(l => l.LevelNumber.ToString().Contains(searchQuery) || l.Name.Contains(searchQuery));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var levels = await query
                .OrderBy(l => l.LevelNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LevelViewModel
                {
                    Id = l.Id,
                    LevelNumber = l.LevelNumber,
                    ExpRequired = l.ExpRequired,
                    Name = l.Name,
                    UserCount = _context.Users.Count(u => u.Id == l.Id),
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            var viewModel = new LevelIndexViewModel
            {
                Levels = levels,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchQuery = searchQuery,
                TotalLevels = totalItems,
                TotalUsers = await _context.Users.CountAsync(),
                ActiveLevels = await _context.Levels.CountAsync(l => l.DeletedAt == null && _context.Users.Any(u => u.LevelId == l.Id))
            };

            return View("~/Views/Admin/Levels/Index.cshtml", viewModel);
        }

        // GET: Levels/Create
        public async Task<IActionResult> Create()
        {
            await EnsureDefaultLevelExistsAsync();
            return View("~/Views/Admin/Levels/Create.cshtml");
        }

        // POST: Levels/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateLevelViewModel model)
        {
            try
            {
                await EnsureDefaultLevelExistsAsync();

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                    return View("~/Views/Admin/Levels/Create.cshtml", model);
                }

                if (model.LevelNumber == 0)
                {
                    TempData["ErrorMessage"] = "Không thể tạo cấp độ 0 vì đây là cấp độ mặc định.";
                    return View("~/Views/Admin/Levels/Create.cshtml", model);
                }

                if (await _context.Levels.AnyAsync(l => l.LevelNumber == model.LevelNumber && l.DeletedAt == null))
                {
                    TempData["ErrorMessage"] = "Số cấp độ này đã tồn tại.";
                    return View("~/Views/Admin/Levels/Create.cshtml", model);
                }

                var level = new Level
                {
                    Id = Guid.NewGuid(),
                    LevelNumber = model.LevelNumber,
                    Name = model.Name,
                    ExpRequired = model.ExpRequired,
                    UpdatedAt = DateTime.Now
                };

                _context.Add(level);
                await _context.SaveChangesAsync();

             

                // Tạo thông báo khi cấp độ được tạo thành công
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Cấp độ mới",
                    Message = $"Cấp độ '{level.Name}' (Level {level.LevelNumber}) đã được tạo thành công.",
                    Type = "success",
                    Icon = "check-circle",
                    Link = $"/Levels/Edit/{level.Id}",
                    IsRead = false
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

                

                TempData["SuccessMessage"] = "Cấp độ đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo cấp độ. Vui lòng thử lại sau.";
                return View("~/Views/Admin/Levels/Create.cshtml", model);
            }
        }

        // GET: Levels/Edit
        public async Task<IActionResult> Edit(Guid id)
        {
            await EnsureDefaultLevelExistsAsync();

            var level = await _context.Levels
                .Where(l => l.Id == id && l.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (level == null)
            {
                return NotFound();
            }

            var viewModel = new EditLevelViewModel
            {
                Id = level.Id,
                LevelNumber = level.LevelNumber,
                Name = level.Name,
                ExpRequired = level.ExpRequired
            };

            return View("~/Views/Admin/Levels/Edit.cshtml", viewModel);
        }

        // POST: Levels/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditLevelViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View("~/Views/Admin/Levels/Edit.cshtml", model);
            }

            try
            {
                await EnsureDefaultLevelExistsAsync();

                var level = await _context.Levels
                    .Where(l => l.Id == id && l.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (level == null)
                {
                    return NotFound();
                }

                if (level.LevelNumber == 0 && (model.LevelNumber != 0 || model.ExpRequired != 0))
                {
                    TempData["ErrorMessage"] = "Không thể chỉnh sửa Level 0. Đây là cấp độ mặc định và phải có kinh nghiệm yêu cầu = 0.";
                    return View("~/Views/Admin/Levels/Edit.cshtml", model);
                }

                if (level.LevelNumber != model.LevelNumber && await _context.Levels.AnyAsync(l => l.LevelNumber == model.LevelNumber && l.DeletedAt == null))
                {
                    TempData["ErrorMessage"] = "Số cấp độ này đã tồn tại.";
                    return View("~/Views/Admin/Levels/Edit.cshtml", model);
                }

                level.LevelNumber = model.LevelNumber;
                level.ExpRequired = model.ExpRequired;
                level.Name = model.Name;
                level.UpdatedAt = DateTime.Now;

                _context.Update(level);
                await _context.SaveChangesAsync();

            

                // Tạo thông báo khi cấp độ được cập nhật
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Cập nhật cấp độ",
                    Message = $"Cấp độ '{level.Name}' (Level {level.LevelNumber}) đã được cập nhật.",
                    Type = "info",
                    Icon = "info-circle",
                    Link = $"/Levels/Edit/{level.Id}",
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

                TempData["SuccessMessage"] = "Cập nhật cấp độ thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật cấp độ. Vui lòng thử lại sau.";
                return View("~/Views/Admin/Levels/Edit.cshtml", model);
            }
        }

        // GET: Levels/Delete
        public async Task<IActionResult> Delete(Guid id)
        {
            await EnsureDefaultLevelExistsAsync();

            var level = await _context.Levels
                .Where(l => l.Id == id && l.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (level == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Levels/Delete.cshtml", level);
        }

        // POST: Levels/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await EnsureDefaultLevelExistsAsync();

                var level = await _context.Levels
                    .Include(l => l.Users)
                    .Where(l => l.Id == id && l.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (level == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy cấp độ để xóa.";
                    return RedirectToAction(nameof(Index));
                }

                if (level.LevelNumber == 0)
                {
                    TempData["ErrorMessage"] = "Không thể xóa Level 0 vì đây là cấp độ mặc định.";
                    return RedirectToAction(nameof(Index));
                }


                if (level.Users.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa cấp độ này vì vẫn còn người dùng liên kết với nó.";
                    return RedirectToAction(nameof(Index));
                }

                // Thực hiện soft delete
                level.DeletedAt = DateTime.UtcNow;
                _context.Update(level);
                await _context.SaveChangesAsync();

           

                // Tạo thông báo khi cấp độ được xóa
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Xóa cấp độ",
                    Message = $"Cấp độ '{level.Name}' (Level {level.LevelNumber}) đã được xóa.",
                    Type = "warning",
                    Icon = "exclamation-circle",
                    Link = $"/Levels",
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



                TempData["SuccessMessage"] = "Xóa cấp độ thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa cấp độ. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}