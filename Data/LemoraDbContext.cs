using System;
using System.IO;
using LEMORA.Models;
using Microsoft.EntityFrameworkCore;

namespace LEMORA.Data
{
    public class LemoraDbContext : DbContext
    {
        public DbSet<ClothingItem> ClothingItems { get; set; }
        public DbSet<Outfit> Outfits => Set<Outfit>();
        public DbSet<OutfitItem> OutfitItems => Set<OutfitItem>();

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
                optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClothingItem>()
                .HasIndex(item => item.Category);

            modelBuilder.Entity<OutfitItem>()
                .HasIndex(item => new { item.OutfitId, item.Slot })
                .IsUnique();

            modelBuilder.Entity<OutfitItem>()
                .HasOne(item => item.Outfit)
                .WithMany(outfit => outfit.Items)
                .HasForeignKey(item => item.OutfitId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OutfitItem>()
                .HasOne(item => item.ClothingItem)
                .WithMany()
                .HasForeignKey(item => item.ClothingItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
