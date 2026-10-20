using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LEMORA.Models;
using LEMORA.Services;

namespace LEMORA.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly ClothingService _clothingService = new();
        private int _totalGarments;
        private int _totalCategories;
        private bool _isLoading;
        private string _errorMessage = "";

        public int TotalGarments
        {
            get => _totalGarments;
            private set
            {
                if (SetProperty(ref _totalGarments, value))
                    OnPropertyChanged(nameof(IsWardrobeEmpty));
            }
        }

        public int TotalCategories
        {
            get => _totalCategories;
            private set => SetProperty(ref _totalCategories, value);
        }

        // These modules will be connected when their database tables exist.
        public int SavedOutfitsCount => 0;
        public int PlannedOutfitsCount => 0;

        public bool IsWardrobeEmpty => TotalGarments == 0;
        public bool HasRecentItems => RecentItems.Count > 0;
        public bool HasCategories => CategoryBreakdown.Count > 0;
        public bool HasNoCategories => !HasCategories;

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<ClothingItem> RecentItems { get; } = new();
        public ObservableCollection<CategorySummary> CategoryBreakdown { get; } = new();

        public async Task RefreshAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            ErrorMessage = "";

            try
            {
                var items = await _clothingService.GetAllAsync();

                RecentItems.Clear();
                foreach (var item in items
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Take(3))
                {
                    RecentItems.Add(item);
                }

                CategoryBreakdown.Clear();
                var groups = items
                    .GroupBy(
                        x => string.IsNullOrWhiteSpace(x.Category)
                            ? "Uncategorized" : x.Category.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .ToList();

                foreach (var group in groups)
                {
                    CategoryBreakdown.Add(new CategorySummary
                    {
                        Category = group.Key,
                        Count = group.Count(),
                        Percentage = items.Count == 0
                            ? 0 : group.Count() * 100.0 / items.Count
                    });
                }

                TotalGarments = items.Count;
                TotalCategories = groups.Count;
                OnPropertyChanged(nameof(HasRecentItems));
                OnPropertyChanged(nameof(HasCategories));
                OnPropertyChanged(nameof(HasNoCategories));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unable to load wardrobe statistics: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class CategorySummary
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Summary => $"{Count} items ({Percentage:0}%)";
    }
}
