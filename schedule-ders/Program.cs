using schedule_ders.Data;
using schedule_ders.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var candidateConnectionStrings = new[]
{
    builder.Configuration["AZURE_SQL_CONNECTIONSTRING"],
    builder.Configuration.GetConnectionString("AzureSqlConnection"),
    builder.Configuration.GetConnectionString("DefaultConnection")
};

var connectionString = candidateConnectionStrings
    .FirstOrDefault(cs => !string.IsNullOrWhiteSpace(cs))
    ?? throw new InvalidOperationException("No SQL connection string configured.");

builder.Services.AddDbContext<ScheduleContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ScheduleContext>();

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
