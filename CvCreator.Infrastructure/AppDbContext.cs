using CvCreator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CvCreator.Infrastructure;

public class AppDbContext(DbContextOptions options) : DbContext(options) 
{
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<CoverLetter> CoverLetters => Set<CoverLetter>();
}
