using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using schedule_ders.Models;

public class ScheduleContext : IdentityDbContext
{
    public ScheduleContext(DbContextOptions<ScheduleContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; }
    public DbSet<Session> Sessions { get; set; }
}
