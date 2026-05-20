using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

using Batarilan_Exercise1.Data;
using Batarilan_Exercise1.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5001");

builder.Services.AddControllersWithViews();

// Session must be registered before building the app
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".CCS.Session";
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    ));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath  = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        context.Users.Add(new UserAccount
        {
            IdNumber    = "ADMIN001",
            FirstName   = "System",
            LastName    = "Administrator",
            MiddleName  = "N/A",
            Email       = "admin@ccs.com",
            Password    = "admin123",
            Course      = "N/A",
            Role        = "Admin",
            CourseLevel = "N/A",
            Address     = "N/A"
        });
        context.SaveChanges();
    }

    // Seed 50 PCs per lab room if not already seeded
    var labs = new[] { "Lab 524", "Lab 526", "Lab 528", "Lab 530", "Lab 542", "Lab 544" };
    foreach (var lab in labs)
    {
        if (!context.PcUnits.Any(p => p.Room == lab))
        {
            for (int i = 1; i <= 50; i++)
            {
                context.PcUnits.Add(new PcUnit
                {
                    Room     = lab,
                    PcNumber = i,
                    Status   = "Available"
                });
            }
        }
    }
    context.SaveChanges();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// ── Correct order: Session → Authentication → Authorization ──
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
