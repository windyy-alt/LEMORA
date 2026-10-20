
using LEMORA.Data;
using LEMORA.Models;
using Microsoft.EntityFrameworkCore;

namespace LEMORA.Services
{
    public class ClothingService
    {
        public async Task<List<ClothingItem>> GetAllAsync()
        {
            await using var db = new LemoraDbContext();

            return await db.ClothingItems
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            await using var db = new LemoraDbContext();

            return await db.ClothingItems.CountAsync();
        }

        public async Task AddAsync(ClothingItem item)
        {
            await using var db = new LemoraDbContext();

            db.ClothingItems.Add(item);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(ClothingItem item)
        {
            await using var db = new LemoraDbContext();

            var existing = await db.ClothingItems.FindAsync(item.Id);

            if (existing == null)
                return;

            existing.Name = item.Name;
            existing.Category = item.Category;
            existing.Brand = item.Brand;
            existing.ColorName = item.ColorName;
            existing.ColorHex = item.ColorHex;
            existing.ImagePath = item.ImagePath;
            existing.OccasionTags = item.OccasionTags;
            existing.SeasonTags = item.SeasonTags;
            existing.Notes = item.Notes;
            existing.IsFavorite = item.IsFavorite;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var db = new LemoraDbContext();

            var item = await db.ClothingItems.FindAsync(id);

            if (item == null)
                return;

            db.ClothingItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
