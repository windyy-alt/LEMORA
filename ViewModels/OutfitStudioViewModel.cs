using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEMORA.Models;
using LEMORA.Services;

namespace LEMORA.ViewModels
{
    public class OutfitSlotSelection : ObservableObject
    {
        private ClothingItem? _item;

        public string Slot { get; }
        public string Label { get; }
        public ClothingItem? Item
        {
            get => _item;
            set
            {
                if (SetProperty(ref _item, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(ImagePath));
                    OnPropertyChanged(nameof(HasItem));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public string DisplayName => Item?.Name ?? "Choose an item";
        public string? ImagePath => Item?.ImagePath;
        public bool HasItem => Item is not null;
        public bool IsEmpty => Item is null;

        public OutfitSlotSelection(string slot, string label)
        {
            Slot = slot;
            Label = label;
        }
    }

    public class OutfitStudioViewModel : ObservableObject
    {
        private readonly ClothingService _clothingService = new();
        private readonly OutfitService _outfitService = new();
        private readonly Dictionary<string, OutfitSlotSelection> _slots;
        private int? _editingOutfitId;

        public ObservableCollection<ClothingItem> WardrobeItems { get; } = new();
        public ObservableCollection<Outfit> SavedOutfits { get; } = new();
        public ObservableCollection<OutfitSlotSelection> PreviewSlots { get; } = new();
        public ICollectionView FilteredClothes { get; }

        public string[] ClothingCategories { get; } =
        {
            "All", "Tops", "Bottoms", "Outerwear", "Dresses", "Shoes", "Accessories"
        };

        public string[] Occasions { get; } =
        {
            "Campus", "Casual", "Formal", "Hangout", "Dinner", "Other"
        };

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    FilteredClothes.Refresh();
            }
        }

        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    FilteredClothes.Refresh();
            }
        }

        private string _outfitName = "";
        public string OutfitName
        {
            get => _outfitName;
            set => SetProperty(ref _outfitName, value);
        }

        private string _selectedOccasion = "Campus";
        public string SelectedOccasion
        {
            get => _selectedOccasion;
            set => SetProperty(ref _selectedOccasion, value);
        }

        private string _feedback = "";
        public string Feedback
        {
            get => _feedback;
            set => SetProperty(ref _feedback, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string EditorTitle => _editingOutfitId.HasValue ? "Editing saved outfit" : "New outfit";
        public string SaveButtonText => _editingOutfitId.HasValue ? "Save Changes" : "Save Outfit";
        public int SelectedCount => _slots.Values.Count(slot => slot.Item is not null);
        public bool IsWardrobeEmpty => WardrobeItems.Count == 0;
        public bool HasSavedOutfits => SavedOutfits.Count > 0;

        public IRelayCommand<ClothingItem> SelectClothingCommand { get; }
        public IRelayCommand<OutfitSlotSelection> RemoveSlotCommand { get; }
        public IRelayCommand<Outfit> OpenSavedOutfitCommand { get; }
        public IRelayCommand NewOutfitCommand { get; }
        public IAsyncRelayCommand SaveOutfitCommand { get; }

        public OutfitStudioViewModel()
        {
            _slots = new Dictionary<string, OutfitSlotSelection>
            {
                ["Outerwear"] = new("Outerwear", "Outerwear"),
                ["Top"] = new("Top", "Top"),
                ["Bottom"] = new("Bottom", "Bottom"),
                ["Shoes"] = new("Shoes", "Shoes"),
                ["Accessories"] = new("Accessories", "Accessory"),
                ["Dress"] = new("Dress", "Dress")
            };

            FilteredClothes = CollectionViewSource.GetDefaultView(WardrobeItems);
            FilteredClothes.Filter = FilterClothing;

            SelectClothingCommand = new RelayCommand<ClothingItem>(SelectClothing);
            RemoveSlotCommand = new RelayCommand<OutfitSlotSelection>(RemoveSlot);
            OpenSavedOutfitCommand = new RelayCommand<Outfit>(OpenSavedOutfit);
            NewOutfitCommand = new RelayCommand(BeginNewOutfit);
            SaveOutfitCommand = new AsyncRelayCommand(SaveOutfitAsync);

            UpdatePreviewSlots();
        }

        private bool FilterClothing(object value)
        {
            if (value is not ClothingItem item)
                return false;

            bool matchesCategory = SelectedCategory == "All" ||
                string.Equals(item.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase);

            bool matchesText = item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                item.ColorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (item.Brand?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

            return matchesCategory && matchesText;
        }

        private static string? GetSlot(string category) => category switch
        {
            "Tops" => "Top",
            "Bottoms" => "Bottom",
            "Outerwear" => "Outerwear",
            "Dresses" => "Dress",
            "Shoes" => "Shoes",
            "Accessories" => "Accessories",
            _ => null
        };

        private void SelectClothing(ClothingItem? clothing)
        {
            if (clothing is null)
                return;

            string? slotName = GetSlot(clothing.Category);
            if (slotName is null)
            {
                Feedback = "This clothing category is not supported in Outfit Studio.";
                return;
            }

            if (slotName == "Dress")
            {
                _slots["Top"].Item = null;
                _slots["Bottom"].Item = null;
            }
            else if (slotName is "Top" or "Bottom")
            {
                _slots["Dress"].Item = null;
            }

            _slots[slotName].Item = clothing;
            UpdatePreviewSlots();
            Feedback = $"Added {clothing.Name} to {slotName}.";
        }

        private void RemoveSlot(OutfitSlotSelection? slot)
        {
            if (slot is null)
                return;

            slot.Item = null;
            UpdatePreviewSlots();
            Feedback = $"Removed {slot.Label.ToLowerInvariant()} from the outfit.";
        }

        private void UpdatePreviewSlots()
        {
            string[] activeSlots = _slots["Dress"].HasItem
                ? new[] { "Outerwear", "Dress", "Shoes", "Accessories" }
                : new[] { "Outerwear", "Top", "Bottom", "Shoes", "Accessories" };

            PreviewSlots.Clear();
            foreach (string key in activeSlots)
                PreviewSlots.Add(_slots[key]);

            OnPropertyChanged(nameof(SelectedCount));
        }

        public void BeginNewOutfit()
        {
            _editingOutfitId = null;
            OutfitName = "";
            SelectedOccasion = "Campus";
            foreach (var slot in _slots.Values)
                slot.Item = null;

            UpdatePreviewSlots();
            Feedback = "Start by selecting clothes from your wardrobe.";
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void OpenSavedOutfit(Outfit? outfit)
        {
            if (outfit is null)
                return;

            BeginNewOutfit();
            _editingOutfitId = outfit.Id;
            OutfitName = outfit.Name;
            SelectedOccasion = outfit.Occasion;

            foreach (var member in outfit.Items)
            {
                if (_slots.TryGetValue(member.Slot, out var slot))
                    slot.Item = WardrobeItems.FirstOrDefault(item => item.Id == member.ClothingItemId);
            }

            UpdatePreviewSlots();
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            Feedback = $"Editing '{outfit.Name}'. You can replace any clothing slot.";
        }

        public async Task RefreshAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            try
            {
                var clothes = await _clothingService.GetAllAsync();
                WardrobeItems.Clear();
                foreach (var item in clothes)
                    WardrobeItems.Add(item);

                FilteredClothes.Refresh();
                OnPropertyChanged(nameof(IsWardrobeEmpty));

                foreach (var slot in _slots.Values)
                {
                    if (slot.Item is not null)
                        slot.Item = clothes.FirstOrDefault(item => item.Id == slot.Item.Id);
                }
                UpdatePreviewSlots();

                await RefreshSavedOutfitsAsync();
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to load Outfit Studio: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshSavedOutfitsAsync()
        {
            var outfits = await _outfitService.GetAllAsync();
            SavedOutfits.Clear();
            foreach (var outfit in outfits)
                SavedOutfits.Add(outfit);
            OnPropertyChanged(nameof(HasSavedOutfits));
        }

        private async Task SaveOutfitAsync()
        {
            try
            {
                var selections = _slots.Values
                    .Where(slot => slot.Item is not null)
                    .ToDictionary(slot => slot.Slot, slot => slot.Item!.Id);

                bool wasEditing = _editingOutfitId.HasValue;
                int savedId = await _outfitService.SaveAsync(
                    _editingOutfitId, OutfitName, SelectedOccasion, selections);

                _editingOutfitId = savedId;
                OnPropertyChanged(nameof(EditorTitle));
                OnPropertyChanged(nameof(SaveButtonText));
                await RefreshSavedOutfitsAsync();
                Feedback = wasEditing ? "Outfit updated successfully!" : "Outfit saved successfully!";
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to save outfit: {ex.Message}";
            }
        }

        public async Task<bool> DeleteOutfitAsync(Outfit? outfit)
        {
            if (outfit is null)
                return false;

            try
            {
                await _outfitService.DeleteAsync(outfit.Id);
                if (_editingOutfitId == outfit.Id)
                    BeginNewOutfit();

                await RefreshSavedOutfitsAsync();
                Feedback = "Outfit deleted successfully!";
                return true;
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to delete outfit: {ex.Message}";
                return false;
            }
        }
    }
}
