using CloudinaryDotNet;
using DotNetTruyen.Data;
using DotNetTruyen.Hubs;
using DotNetTruyen.Models;
using DotNetTruyen.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); // Nếu muốn loại bỏ các nhà cung cấp logging mặc định
builder.Logging.AddConsole(); // Thêm log ra Console
builder.Logging.AddDebug();
builder.Services.AddSignalR();

builder.Services.AddHostedService<ChapterPublishWorker>();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<EmailService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserLevelService>();
builder.Services.AddScoped<IPhoToService, PhotoService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddSingleton(provider =>
{
    var cloudinaryAccount = new Account(
        builder.Configuration["CloudinarySettings:CloudName"],
        builder.Configuration["CloudinarySettings:ApiKey"],
        builder.Configuration["CloudinarySettings:ApiSecret"]
                         );
    var cloudinary = new Cloudinary(cloudinaryAccount);
    return cloudinary;
});
builder.Services.AddDbContext<DotNetTruyenDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});


builder.Services.AddIdentity<User, IdentityRole<Guid>>().AddEntityFrameworkStores<DotNetTruyenDbContext>().AddDefaultTokenProviders().AddErrorDescriber<CustomIdentityErrorDescriber>();

// Truy cập IdentityOptions
builder.Services.Configure<IdentityOptions>(options => {

    // Cấu hình về User.
    options.User.AllowedUserNameCharacters = // các ký tự đặt tên user
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;  // Email là duy nhất


    // Cấu hình đăng nhập.
    options.SignIn.RequireConfirmedEmail = true;            // Cấu hình xác thực địa chỉ email (email phải tồn tại)
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login/";
    options.LogoutPath = "/logout/";
    options.AccessDeniedPath = "/accessDenied";
});

builder.Services.AddAuthentication()
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication_Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication_Google:ClientSecret"];
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessDashboard", policy =>
        policy.RequireClaim("Permission", "Vào bảng điều khiển"));

    options.AddPolicy("CanManageUser", policy =>
        policy.RequireClaim("Permission", "Quản lý người dùng"));

    options.AddPolicy("CanManageRole", policy =>
        policy.RequireClaim("Permission", "Quản lý vai trò"));

    options.AddPolicy("CanManageComic", policy =>
        policy.RequireClaim("Permission", "Quản lý truyện"));

    options.AddPolicy("CanManageChapter", policy =>
        policy.RequireClaim("Permission", "Quản lý chương"));

    options.AddPolicy("CanManageGenre", policy =>
        policy.RequireClaim("Permission", "Quản lý thể loại"));

    options.AddPolicy("CanManageNotification", policy =>
        policy.RequireClaim("Permission", "Quản lý thông báo"));

    options.AddPolicy("CanManageAdvertise", policy =>
        policy.RequireClaim("Permission", "Quản lý quảng cáo"));

    options.AddPolicy("CanManageRank", policy =>
        policy.RequireClaim("Permission", "Quản lý xếp hạng"));
});


ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
var app = builder.Build();
app.MapControllerRoute(
    name: "comicDetail",
    pattern: "Comic/Detail/{id}",
    defaults: new { controller = "Detail", action = "Index" }
);

//using (var scope = app.Services.CreateScope())
//{
//    await SeedData.Initialize(scope.ServiceProvider);
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/signin-google" && context.Request.Query.ContainsKey("error"))
    {
        context.Response.Redirect("/");
        return;
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ComicHub>("/comicHub");
app.MapHub<CommentHub>("/commentHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
