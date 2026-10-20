using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LEMORA.Data;
using LEMORA.Models;
using Microsoft.EntityFrameworkCore;

namespace LEMORA.Services
{
    public class OutfitService
    {
        private static readonly Dictionary<string, string> SlotCategories = new()
        {
            ["Top"] = "Tops",
            ["Bottom"] = "Bottoms",
            ["Outerwear"] = "Outerwear",
            ["Shoes"] = "Shoes",
            ["Accessories"] = "Accessories",
            ["Dress"] = "Dresses"
        };

        public async Task<List<Outfit>> GetAllAsync()
        {
            await using var db = new LemoraDbContext();
            return await db.Outfits
                .Include(outfit => outfit.Items)
                .ThenInclude(item => item.ClothingItem)
                .AsNoTracking()
                .OrderByDescending(outfit => outfit.CreatedAt)
                .ThenByDescending(outfit => outfit.Id)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            await using var db = new LemoraDbContext();
            return await db.Outfits.CountAsync();
        }

        public async Task<int> SaveAsync(
            int? outfitId,
            string name,
            string occasion,
            IReadOnlyDictionary<string, int> selectedItems)
        {
            name = name.Trim();
            occasion = occasion.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                throw new InvalidOperationException("Outfit name is required (max. 100 characters).");

            if (string.IsNullOrWhiteSpace(occasion) || occasion.Length > 40)
                throw new InvalidOperationException("Choose an occasion.");

            bool isDressLook = selectedItems.ContainsKey("Dress");
            if (isDressLook && (selectedItems.ContainsKey("Top") || selectedItems.ContainsKey("Bottom")))
                throw new InvalidOperationException("A dress cannot be combined with a separate top or bottom.");

            if (!isDressLook && (!selectedItems.ContainsKey("Top") || !selectedItems.ContainsKey("Bottom")))
                throw new InvalidOperationException("Select a top and bottom, or a dress, before saving.");

            if (selectedItems.Values.Distinct().Count() != selectedItems.Count)
                throw new InvalidOperationException("The same clothing item cannot fill multiple slots.");

            await using var db = new LemoraDbContext();
            var selectedIds = selectedItems.Values.ToArray();
            var existingClothes = await db.ClothingItems
                .Where(item => selectedIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id);

            foreach (var (slot, clothingId) in selectedItems)
            {
                if (!SlotCategories.TryGetValue(slot, out var expectedCategory) ||
                    !existingClothes.TryGetValue(clothingId, out var clothing) ||
                    !string.Equals(clothing.Category, expectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("An outfit item is missing or does not match its slot.");
                }
            }

            if (outfitId is null)
            {
                var outfit = new Outfit
                {
                    Name = name,
                    Occasion = occasion,
                    CreatedAt = DateTime.Now,
                    Items = selectedItems.Select(pair => new OutfitItem
                    {
                        Slot = pair.Key,
                        ClothingItemId = pair.Value
                    }).ToList()
                };

                db.Outfits.Add(outfit);
                await db.SaveChangesAsync();
                return outfit.Id;
            }

            // Remove old slot rows first to avoid the unique (OutfitId, Slot) index.
            await using var transaction = await db.Database.BeginTransactionAsync();
            var current = await db.Outfits
                .Include(outfit => outfit.Items)
                .FirstOrDefaultAsync(outfit => outfit.Id == outfitId.Value);

            if (current is null)
                throw new InvalidOperationException("The selected outfit no longer exists.");

            current.Name = name;
            current.Occasion = occasion;

            db.OutfitItems.RemoveRange(current.Items);
            await db.SaveChangesAsync();

            foreach (var (slot, clothingId) in selectedItems)
            {
                db.OutfitItems.Add(new OutfitItem
                {
                    OutfitId = current.Id,
                    ClothingItemId = clothingId,
                    Slot = slot
                });
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return current.Id;
        }

        public async Task DeleteAsync(int outfitId)
        {
            await using var db = new LemoraDbContext();
            var outfit = await db.Outfits.FindAsync(outfitId);
            if (outfit is null)
                throw new InvalidOperationException("The selected outfit no longer exists.");

            bool hasSchedules = await db.OutfitPlans.AnyAsync(plan => plan.OutfitId == outfitId);
            if (hasSchedules)
                throw new InvalidOperationException(
                    "This outfit is scheduled in Outfit Planner. Remove its plans before deleting it.");

            db.Outfits.Remove(outfit);
            await db.SaveChangesAsync();
        }
    }
}
