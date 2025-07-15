using DotNetTruyen.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetTruyen.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var dbContext = scope.ServiceProvider.GetRequiredService<DotNetTruyenDbContext>();

                await dbContext.Database.MigrateAsync(); // Đảm bảo DB đã được migrate


                var adminRole = await roleManager.FindByNameAsync("Admin");
                if (adminRole == null)
                {
                    adminRole = new IdentityRole<Guid> { Name = "Admin", NormalizedName = "ADMIN" };
                    await roleManager.CreateAsync(adminRole);
                }

                var readerRole = await roleManager.FindByNameAsync("Reader");
                if (readerRole == null)
                {
                    readerRole = new IdentityRole<Guid> { Name = "Reader", NormalizedName = "READER" };
                    await roleManager.CreateAsync(readerRole);
                }

                adminRole = await roleManager.FindByNameAsync("Admin");
                if (adminRole == null)
                {
                    throw new Exception("Admin role creation failed!");
                }
                Guid adminRoleId = adminRole.Id;

                // Tạo tài khoản admin
                string adminEmail = "admin@example.com";
                string adminPassword = "Admin@123"; 
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        EmailConfirmed = true,
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        adminUser.LockoutEnabled = false;
                        await userManager.UpdateAsync(adminUser);
                    }
                }

                // Seed quyền (Claims) cho Admin Role
                var roleClaims = new (string ClaimType, string ClaimValue)[]
                {
                ("Permission", "Vào bảng điều khiển"),
                ("Permission", "Quản lý người dùng"),
                ("Permission", "Quản lý vai trò"),
                ("Permission", "Quản lý truyện"),
                ("Permission", "Quản lý chương"),
                ("Permission", "Quản lý thể loại"),
                ("Permission", "Quản lý thông báo"),
                ("Permission", "Quản lý quảng cáo"),
                ("Permission", "Quản lý xếp hạng")
                };

                foreach (var claim in roleClaims)
                {
                    var existingClaim = await dbContext.RoleClaims.FirstOrDefaultAsync(rc =>
                        rc.RoleId == adminRoleId && rc.ClaimType == claim.ClaimType && rc.ClaimValue == claim.ClaimValue);

                    if (existingClaim == null)
                    {
                        await dbContext.RoleClaims.AddAsync(new IdentityRoleClaim<Guid>
                        {
                            RoleId = adminRoleId,
                            ClaimType = claim.ClaimType,
                            ClaimValue = claim.ClaimValue
                        });
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
