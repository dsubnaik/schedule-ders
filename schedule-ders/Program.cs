using schedule_ders.Data;
using schedule_ders.Infrastructure;
using schedule_ders.Models;
using schedule_ders.Services;
using schedule_ders.Services.Interfaces;
using schedule_ders.Utilities;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Trust Railway's proxy so X-Forwarded-For contains the real client IP.
// KnownNetworks/KnownProxies are cleared because Railway's internal proxy
// IP is not fixed — this is safe because Railway controls the edge.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Reject request bodies larger than 512 KB before they reach any controller.
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 512 * 1024);

// Cap individual form field values at 16 KB and total form body at 512 KB.
builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = 16 * 1024;
    o.MultipartBodyLengthLimit = 512 * 1024;
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Trim leading/trailing whitespace from every string bound through model binding.
    options.ModelBinderProviders.Insert(0, new TrimStringModelBinderProvider());
});
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = DatabaseConnectionStringResolver.ResolveConfigurationConnectionString(builder.Configuration);

builder.Services.AddDbContext<ScheduleContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lock account for 15 minutes after 5 consecutive failed logins.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ScheduleContext>();

builder.Services.AddSingleton<IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.NoOpEmailSender>();
builder.Services.AddScoped<IScheduleQueryService, ScheduleQueryService>();
builder.Services.AddScoped<IProfessorRequestService, ProfessorRequestService>();
builder.Services.AddScoped<IAdminRequestService, AdminRequestService>();


var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
    var db = migrationScope.ServiceProvider.GetRequiredService<ScheduleContext>();
    await db.Database.MigrateAsync();

    var connection = db.Database.GetDbConnection();
    app.Logger.LogInformation(
        "ScheduleContext connected to DataSource='{DataSource}', Database='{Database}'",
        connection.DataSource,
        connection.Database);
}

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

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

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
