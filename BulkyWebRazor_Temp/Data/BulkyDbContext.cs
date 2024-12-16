using BulkyWebRazor_Temp.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyWebRazor_Temp.Data
{
    public class BulkyDbContext : DbContext
    {
        public BulkyDbContext(DbContextOptions<BulkyDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id=4, Name="Spooky", DisplayOrder=1},
                new Category { Id=5, Name="Very Sppoky", DisplayOrder=1},
                new Category { Id=6, Name="Super Spooky", DisplayOrder=1}
                );
        }

    }
}
