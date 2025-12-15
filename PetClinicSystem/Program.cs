using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using QuestPDF.Infrastructure;   // <-- REQUIRED FOR PDF

var builder = WebApplication.CreateBuilder(args);

// REQUIRED FOR QUESTPDF
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// MySQL Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Enable Sessions
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
