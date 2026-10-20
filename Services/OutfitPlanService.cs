using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LEMORA.Data;
using LEMORA.Models;
using Microsoft.EntityFrameworkCore;

namespace LEMORA.Services
{
    public class OutfitPlanService
    {
        public async Task<List<OutfitPlan>> GetAllAsync()
        {
            await using var db = new LemoraDbContext();
            return await db.OutfitPlans
                .Include(plan => plan.Outfit)
                .ThenInclude(outfit => outfit.Items)
                .ThenInclude(item => item.ClothingItem)
                .AsNoTracking()
                .OrderBy(plan => plan.Date)
                .ToListAsync();
        }

        public async Task<int> GetUpcomingCountAsync()
        {
            await using var db = new LemoraDbContext();
            DateTime today = DateTime.Today;
            return await db.OutfitPlans.CountAsync(plan => plan.Date >= today);
        }

        public async Task<OutfitPlan?> GetForDateAsync(DateTime date)
        {
            await using var db = new LemoraDbContext();
            DateTime targetDate = date.Date;
            return await db.OutfitPlans
                .Include(plan => plan.Outfit)
                .ThenInclude(outfit => outfit.Items)
                .ThenInclude(item => item.ClothingItem)
                .AsNoTracking()
                .FirstOrDefaultAsync(plan => plan.Date == targetDate);
        }

        public async Task SaveAsync(DateTime date, int outfitId, string activity, string? notes)
        {
            DateTime day = date.Date;
            activity = activity.Trim();
            notes = notes?.Trim();

            if (activity.Length == 0 || activity.Length > 100)
                throw new InvalidOperationException("Enter an activity name (max. 100 characters).");

            if (notes is { Length: > 300 })
                throw new InvalidOperationException("Notes must be 300 characters or less.");

            await using var db = new LemoraDbContext();
            if (!await db.Outfits.AnyAsync(outfit => outfit.Id == outfitId))
                throw new InvalidOperationException("The selected outfit no longer exists.");

            var existing = await db.OutfitPlans.FirstOrDefaultAsync(plan => plan.Date == day);
            if (existing is null)
            {
                db.OutfitPlans.Add(new OutfitPlan
                {
                    Date = day,
                    OutfitId = outfitId,
                    Activity = activity,
                    Notes = notes
                });
            }
            else
            {
                existing.OutfitId = outfitId;
                existing.Activity = activity;
                existing.Notes = notes;
            }

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(DateTime date)
        {
            await using var db = new LemoraDbContext();
            DateTime day = date.Date;
            var plan = await db.OutfitPlans.FirstOrDefaultAsync(x => x.Date == day);
            if (plan is null)
                throw new InvalidOperationException("No scheduled outfit exists on that date.");

            db.OutfitPlans.Remove(plan);
            await db.SaveChangesAsync();
        }
    }
}
