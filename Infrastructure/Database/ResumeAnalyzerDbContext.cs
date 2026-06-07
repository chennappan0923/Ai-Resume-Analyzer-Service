using AI_Resume_Analyzer_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI_Resume_Analyzer_API.Infrastructure.Database
{
    public class ResumeAnalyzerDbContext : DbContext
    {
        public ResumeAnalyzerDbContext(DbContextOptions<ResumeAnalyzerDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Resume> Resumes => Set<Resume>();
        public DbSet<ResumeAnalysis> ResumeAnalyses => Set<ResumeAnalysis>();
        public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Resume configuration
            modelBuilder.Entity<Resume>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ResumeName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(250);
                entity.Property(e => e.FilePath).IsRequired();
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Resumes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ResumeAnalysis configuration
            modelBuilder.Entity<ResumeAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CareerLevel).HasMaxLength(50);
                
                entity.HasOne(e => e.Resume)
                    .WithOne(r => r.Analysis)
                    .HasForeignKey<ResumeAnalysis>(e => e.ResumeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // InterviewQuestion configuration
            modelBuilder.Entity<InterviewQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Question).IsRequired();

                entity.HasOne(e => e.ResumeAnalysis)
                    .WithMany(a => a.InterviewQuestions)
                    .HasForeignKey(e => e.ResumeAnalysisId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
