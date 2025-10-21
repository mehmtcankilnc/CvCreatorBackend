using CvCreator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CvCreator.Infrastructure;

public class AppDbContext(DbContextOptions options) : DbContext(options) 
{
    public DbSet<Resume> Resumes => Set<Resume>();
}
