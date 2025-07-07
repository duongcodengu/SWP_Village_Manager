using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;

var builder = WebApplication.CreateBuilder(args);

// SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);


builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // ton tai trong 60 phut
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.LogoutPath = "/logout";
    });

// cau hinh email 
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Enable session support
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();