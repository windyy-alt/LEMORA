
using System;
using System.IO;
using LEMORA.Models;
using Microsoft.EntityFrameworkCore;

namespace LEMORA.Data
{
    public class LemoraDbContext : DbContext
    {
        public DbSet<ClothingItem> ClothingItems { get; set; }

        public static string DatabasePath =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "LEMORA",
                "lemora.db");

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string directory = Path.GetDirectoryName(DatabasePath)!;

                Directory.CreateDirectory(directory);

                optionsBuilder.UseSqlite(
                    $"Data Source={DatabasePath}");
            }
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClothingItem>()
                .HasIndex(item => item.Category);
        }
    }
}
