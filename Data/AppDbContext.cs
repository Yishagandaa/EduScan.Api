using Microsoft.EntityFrameworkCore;
using EduScan.Api.Models;

namespace EduScan.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure unique index on LRN
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(s => s.Lrn).IsUnique();
            entity.Property(s => s.Lrn).IsRequired().HasMaxLength(50);
            entity.Property(s => s.FullName).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<AttendanceLog>(entity =>
        {
            entity.Property(a => a.StudentLrn).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Status).IsRequired().HasMaxLength(50);
            entity.Property(a => a.GateNumber).HasMaxLength(50);
        });

        // Seed default test student
        modelBuilder.Entity<Student>().HasData(
            new Student
            {
                Id = 1,
                Lrn = "108492190042",
                FullName = "DELA CRUZ, JUAN MIGUEL B.",
                Grade = "Grade 11",
                Section = "Sampaguita",
                Track = "TVL - Agri-Fishery Arts",
                GuardianPhone = "0917-849-2104",
                IsActiveUser = false,
                LastActiveAt = null
            }
        );
    }
}
