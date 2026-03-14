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
    public DbSet<CourseCatalogEntry> CourseCatalogEntries { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<SIRequest> SIRequests { get; set; }
    public DbSet<SIRequestLeaderCandidate> SIRequestLeaderCandidates { get; set; }
    public DbSet<SILeader> SILeaders { get; set; }
    public DbSet<StudentFavoriteCourse> StudentFavoriteCourses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StudentFavoriteCourse>()
            .HasIndex(f => new { f.UserId, f.CourseID })
            .IsUnique();

        builder.Entity<CourseCatalogEntry>()
            .HasIndex(c => c.CourseCrn)
            .IsUnique();

        builder.Entity<Semester>()
            .HasIndex(s => s.SemesterCode)
            .IsUnique();

        builder.Entity<SILeader>()
            .HasIndex(l => l.ANumber)
            .IsUnique();

        builder.Entity<SIRequestLeaderCandidate>()
            .HasOne(c => c.SIRequest)
            .WithMany(r => r.LeaderCandidates)
            .HasForeignKey(c => c.SIRequestID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
