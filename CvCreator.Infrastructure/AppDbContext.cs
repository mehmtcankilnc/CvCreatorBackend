using CvCreator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvCreator.Infrastructure;

public class AppDbContext(DbContextOptions options) : DbContext(options) 
{
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<CoverLetter> CoverLetters => Set<CoverLetter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resume>()
            .Property(r => r.ResumeFormValues)
            .HasColumnType("jsonb");

        modelBuilder.Entity<CoverLetter>()
            .Property(c => c.CoverLetterFormValues)
            .HasColumnType("jsonb");
    }
}
