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
using DotNetTruyen.ViewModels.Management;
using Microsoft.AspNetCore.Identity;
using DotNetTruyen.Services;
using System.Drawing.Printing;
using static System.Net.WebRequestMethods;
using DotNetTruyen.ViewModels;
using OfficeOpenXml;

namespace DotNetTruyen.Controllers.Admin.UserManagement
{
    [Authorize(Policy = "CanManageAdvertise")]
    public class UsersController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly EmailService _emailService;
        private readonly OtpService _otpService;

        public UsersController(
            DotNetTruyenDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            EmailService emailService,
            OtpService otpService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _otpService = otpService;
        }

        [HttpGet("/userManagement")]
        // GET: Users
        public async Task<IActionResult> Index(string searchQuery = "", int page = 1)
        {
            int pageSize = 5;
            var roles = await _context.Roles.Where(r => r.Name != "Admin").Select(r => r.Name).ToListAsync();
            var userRoles = _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            var viewModel = _context.Users
                .Select(u => new UserViewModel
                {
                    Id = u.Id.ToString(),
                    ImageUrl = u.ImageUrl,
                    NameToDisplay = u.NameToDisplay,
                    Email = u.Email,
                    Role = userRoles.ContainsKey(u.Id) ? string.Join(", ", userRoles[u.Id]) : "Không có",
                    Status = u.LockoutEnd,

                });

            if (!string.IsNullOrEmpty(searchQuery))
            {
                viewModel = viewModel.Where(u => u.NameToDisplay.Contains(searchQuery) || u.Email.Contains(searchQuery));
            }

            var totalUsers = await viewModel.CountAsync();

            var users = await viewModel
            .OrderBy(u => u.NameToDisplay)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModel = new UserIndexViewModel
            {
                Users = users,
                SearchQuery = searchQuery,
                CurrentPage = page,
                Roles = roles,
                TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize)
            };
            return View("~/Views/Admin/Users/Index.cshtml", userViewModel);
        }

        [HttpGet("/blockUser")]
        public async Task<IActionResult> BlockUser(string userId)
        {


            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                // Khóa tài khoản vô thời hạn
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "Đã khóa tài khoản";
                return RedirectToAction("Index");
            }
            TempData["ErrorMessage"] = "Khóa tài khoản không thành công";
            return RedirectToAction("Index");
        }

        [HttpGet("/unblockUser")]
        public async Task<IActionResult> UnblockUser(string userId)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Mở khóa user
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "Mở khóa tài khoản thành công";
                return RedirectToAction("Index");
            }
            TempData["ErrorMessage"] = "Mở khóa tài khoản không thành công";
            return RedirectToAction("Index");
        }

        [HttpGet("/resetPassword")]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var newPassword = "A@a" + _otpService.RandomOtp();
                await _emailService.SendEmailAsync(user.Email, "Đặt lại mật khẩu", $"Mật khẩu mới của bạn là: <b>{newPassword}</b> .Sau khi đăng nhập vui lòng đổi lại mật khẩu.");
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, code, newPassword);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Đã đặt lại mật khẩu";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Đặt lại mật khẩu không thành công";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(string userId, List<string> SelectedRoles)
        {
            if (SelectedRoles.Contains("Admin"))
            {
                SelectedRoles.Remove("Admin");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (SelectedRoles.Any())
                {
                    await _userManager.AddToRolesAsync(user, SelectedRoles);
                }

                TempData["SuccessMessage"] = "Cập nhật vai trò thành công!";
                return RedirectToAction("Index");
            }

            ViewData["ShowModal"] = "true";
            TempData["ErrorMessage"] = "Cập nhật vai trò không thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet("/exportUsersToExcel")]
        public async Task<IActionResult> ExportUsersToExcel(string searchQuery = "")
        {

            try
            {
                var usersQuery = _userManager.Users.AsQueryable();
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    usersQuery = usersQuery.Where(u => u.NameToDisplay.Contains(searchQuery) || u.Email.Contains(searchQuery));
                }
                var users = await usersQuery.ToListAsync();
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Users");

                    // Tạo tiêu đề cột
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Tên hiển thị";
                    worksheet.Cells[1, 3].Value = "Tên đăng nhập";
                    worksheet.Cells[1, 4].Value = "Email";
                    worksheet.Cells[1, 5].Value = "Vai trò";
                    worksheet.Cells[1, 6].Value = "Trạng thái";

                    int row = 2;

                    foreach (var user in users)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        string roleString = string.Join(", ", roles);

                        worksheet.Cells[row, 1].Value = user.Id;
                        worksheet.Cells[row, 2].Value = user.NameToDisplay;
                        worksheet.Cells[row, 3].Value = user.UserName;
                        worksheet.Cells[row, 4].Value = user.Email;
                        worksheet.Cells[row, 5].Value = roleString;
                        worksheet.Cells[row, 6].Value = (user.LockoutEnd == null) ? "Hoạt động" : "Khóa";

                        row++;
                    }

                    // Định dạng cột
                    worksheet.Cells.AutoFitColumns();

                    // Xuất file Excel
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = "Users.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return File(stream, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã có lỗi khi xuất file!";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportUsersFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một file Excel hợp lệ.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Bỏ qua dòng tiêu đề
                        {
                            var userId = worksheet.Cells[row, 1].Text;
                            var displayName = worksheet.Cells[row, 2].Text;
                            var username = worksheet.Cells[row, 3].Text;
                            var password = worksheet.Cells[row, 4].Text;
                            var email = worksheet.Cells[row, 5].Text;
                            var roles = worksheet.Cells[row, 6].Text.Split(',').Select(r => r.Trim()).ToList();
                            var status = worksheet.Cells[row, 7].Text;

                            // Kiểm tra email có tồn tại không
                            var existingUser = await _userManager.FindByEmailAsync(email);
                            if (existingUser != null)
                            {
                                TempData["SuccessMessage"] = "Các người dùng đã có trong hệ thống sẽ không được thêm vào";
                                continue; // Bỏ qua nếu user đã tồn tại
                            }

                            var user = new User
                            {
                                UserName = username,
                                Email = email,
                                NameToDisplay = displayName,
                                LockoutEnd = (status == "Hoạt động") ? null : DateTimeOffset.MaxValue,
                                EmailConfirmed = true
                            };
                            IdentityResult result;

                            if (string.IsNullOrEmpty(password))
                            {
                                result = await _userManager.CreateAsync(user, "DefaultPassword123!"); // Tạo user với mật khẩu mặc định
                            }
                            else
                            {
                                result = await _userManager.CreateAsync(user, password); // Tạo user với mật khẩu do người dùng cung cấp
                            }

                            if (result.Succeeded)
                            {
                                await _userManager.AddToRolesAsync(user, roles); // Gán vai trò cho user
                                TempData["SuccessMessage"] = "Nhập dữ liệu từ Excel thành công";
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Lỗi nhập dữ liệu từ Excel";
                            }
                        }
                    }
                }
                return RedirectToAction("Index"); // Chuyển hướng về danh sách user
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi nhập dữ liệu từ Excel";
                return RedirectToAction("Index");
            }
        }
    }
}
