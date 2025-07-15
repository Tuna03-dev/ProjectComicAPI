using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.ViewModels.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace DotNetTruyen.Controllers.Admin.RoleManagement
{
    [Authorize(Policy = "CanManageRole")]
    public class RolesController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public RolesController(DotNetTruyenDbContext context, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        [HttpGet("/roleManagement")]
        public async Task<IActionResult> Index(string searchQuery = "", int page = 1)
        {
            int pageSize = 5;

            var rolesQuery = _context.Roles
            .Select(role => new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Permission = string.Join(", ",
                    _context.RoleClaims
                        .Where(rc => rc.RoleId == role.Id)
                        .Select(rc => rc.ClaimValue)
                        .ToList()) // Lấy danh sách claim của role
            });

            if (!string.IsNullOrEmpty(searchQuery))
            {
                rolesQuery = rolesQuery.Where(r => r.Name.Contains(searchQuery));
            }

            var totalRoles = await rolesQuery.CountAsync();

            var roles = await rolesQuery
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new RoleIndexViewModel
            {
                Roles = roles,
                SearchQuery = searchQuery,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalRoles / (double)pageSize)
            };

            return View("~/Views/Admin/Roles/Index.cshtml", viewModel);
        }

        // GET: Genres/Create
        public async Task<IActionResult> Create()
        {

            var adminRole = _context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var permission = _context.RoleClaims
                        .Where(rc => rc.RoleId == adminRole.Id)
                        .Select(rc => rc.ClaimValue)
                        .ToList();
            // Prepare ViewModel
            var viewModel = new CreateRoleViewModel
            {
                SelectedPermission = new(),
                Permissions = permission
            };

            return View("~/Views/Admin/Roles/Create.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {

            var role = await _context.Roles
                .FirstOrDefaultAsync(g => g.Name == model.Name);

            if (model.SelectedPermission == null)
            {
                model.SelectedPermission = new();
            }

            if (role != null)
            {
                TempData["ErrorMessage"] = "Vai trò đã tồn tại";
                return View("~/Views/Admin/Roles/Create.cshtml", model);
            }

            var newRole = new IdentityRole<Guid> { Name = model.Name };
            var result = await _roleManager.CreateAsync(newRole);

            if (result.Succeeded)
            {
                if (model.SelectedPermission != null)
                {
                    foreach (var claim in model.SelectedPermission)
                    {
                        await _roleManager.AddClaimAsync(newRole, new Claim("Permission", claim));
                    }
                }
                TempData["SuccessMessage"] = "Tạo vai trò mới thành công";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = "Tạo vai trò mới không thành công";
            }

            return View("~/Views/Admin/Roles/Edit.cshtml", model);
        }

        // GET: Genres/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(g => g.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            if (role.Name == "Admin" || role.Name == "Reader")
            {
                return RedirectToAction("Index");
            }

            var adminRole = _context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var permission = _context.RoleClaims
                        .Where(rc => rc.RoleId == adminRole.Id)
                        .Select(rc => rc.ClaimValue)
                        .ToList();

            var selectedPermission = _context.RoleClaims
                        .Where(rc => rc.RoleId == id)
                        .Select(rc => rc.ClaimValue)
                        .ToList();
            // Prepare ViewModel
            var viewModel = new EditRoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                SelectedPermission = selectedPermission,
                Permissions = permission
            };

            return View("~/Views/Admin/Roles/Edit.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditRoleViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(g => g.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            if (role.Name == "Admin" || role.Name == "Reader")
            {
                return RedirectToAction("Index");
            }

            if (model.SelectedPermission == null)
            {
                model.SelectedPermission = new();
            }


            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                var existingClaims = await _roleManager.GetClaimsAsync(role);


                foreach (var claim in existingClaims)
                {
                    await _roleManager.RemoveClaimAsync(role, claim);
                }

                if (model.SelectedPermission.Count > 0)
                {
                    foreach (var claim in model.SelectedPermission)
                    {
                        await _roleManager.AddClaimAsync(role, new Claim("Permission", claim));
                    }
                }
                TempData["SuccessMessage"] = "Thay đổi vai trò thành công";
                return RedirectToAction(nameof(Index));
            }
            else 
            {
                TempData["ErrorMessage"] = "Thay đổi vai trò không thành công";
            }
            return View("~/Views/Admin/Roles/Edit.cshtml", model);
        }

        // GET: Roles/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
            {
                return NotFound();
            }

            if (role.Name == "Admin" || role.Name == "Reader")
            {
                TempData["ErrorMessage"] = "Xóa vai trò không thành công";
                return RedirectToAction("Index");
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);


            foreach (var claim in existingClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
            await _roleManager.DeleteAsync(role);
            TempData["SuccessMessage"] = "Xóa vai trò thành công";
            return RedirectToAction("Index");
        }
    }
}
