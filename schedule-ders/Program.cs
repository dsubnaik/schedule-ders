using schedule_ders.Data;
using schedule_ders.Models;
using schedule_ders.Options;
using schedule_ders.Services;
using schedule_ders.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ScheduleContext>();

builder.Services.Configure<SendGridOptions>(
    builder.Configuration.GetSection(SendGridOptions.SectionName));
builder.Services.AddHttpClient<IEmailSender, SendGridEmailSender>();
builder.Services.AddScoped<IScheduleQueryService, ScheduleQueryService>();
builder.Services.AddScoped<IProfessorRequestService, ProfessorRequestService>();
builder.Services.AddScoped<IAdminRequestService, AdminRequestService>();

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Environment.IsDevelopment());
var removedDuplicateSessions = await SessionDeduper.DeduplicateAsync(app.Services);
if (removedDuplicateSessions > 0)
{
    app.Logger.LogInformation("Removed {Count} duplicate session rows during startup cleanup.", removedDuplicateSessions);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
