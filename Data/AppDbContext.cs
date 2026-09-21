using Microsoft.EntityFrameworkCore;
using AltriumRecruitmentSystem.Models;

namespace AltriumRecruitmentSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Vacancy> Vacancies { get; set; }
        public DbSet<JobApplication> Applications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<JobApplication>()
                .HasIndex(a => new { a.UserId, a.VacancyId })
                .IsUnique();

            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Application)
                .WithOne(a => a.Interview)
                .HasForeignKey<Interview>(i => i.ApplicationId);
        }
    }
}