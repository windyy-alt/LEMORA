using System;
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
    public class MyClosetViewModel : ObservableObject
    {
        private readonly ClothingService _service = new();
        private int? _editingItemId;

        public ObservableCollection<ClothingItem> Items { get; } = new();
        public ICollectionView FilteredItems { get; }

        public string[] ClothingCategories { get; } =
        {
            "Tops", "Bottoms", "Outerwear", "Dresses", "Shoes", "Accessories"
        };

        public string[] Categories { get; } =
        {
            "All", "Tops", "Bottoms", "Outerwear", "Dresses", "Shoes", "Accessories"
        };

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    RefreshFilter();
            }
        }

        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    RefreshFilter();
            }
        }

        private bool _isFormOpen;
        public bool IsFormOpen
        {
            get => _isFormOpen;
            set => SetProperty(ref _isFormOpen, value);
        }

        private string _newName = "";
        public string NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }

        private string _newCategory = "Tops";
        public string NewCategory
        {
            get => _newCategory;
            set => SetProperty(ref _newCategory, value);
        }

        private string _newColor = "";
        public string NewColor
        {
            get => _newColor;
            set => SetProperty(ref _newColor, value);
        }

        private string _newImagePath = "";
        public string NewImagePath
        {
            get => _newImagePath;
            set => SetProperty(ref _newImagePath, value);
        }

        private string _feedback = "";
        public string Feedback
        {
            get => _feedback;
            set => SetProperty(ref _feedback, value);
        }

        public int TotalCount => Items.Count;
        public bool IsEmpty => FilteredItems.IsEmpty;
        public string FormTitle => _editingItemId.HasValue ? "Edit Clothing" : "Add New Clothing";
        public string SaveButtonLabel => _editingItemId.HasValue ? "Save Changes" : "Save Clothing";

        public IRelayCommand OpenFormCommand { get; }
        public IRelayCommand<ClothingItem> EditCommand { get; }
        public IRelayCommand CancelFormCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }

        public MyClosetViewModel()
        {
            FilteredItems = CollectionViewSource.GetDefaultView(Items);
            FilteredItems.Filter = FilterItem;

            OpenFormCommand = new RelayCommand(OpenAddForm);
            EditCommand = new RelayCommand<ClothingItem>(OpenEditForm);
            CancelFormCommand = new RelayCommand(CancelForm);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        private bool FilterItem(object obj)
        {
            if (obj is not ClothingItem item)
                return false;

            bool matchesSearch =
                item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || item.ColorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || (item.Brand?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

            bool matchesCategory = SelectedCategory == "All"
                || item.Category == SelectedCategory;

            return matchesSearch && matchesCategory;
        }

        private void RefreshFilter()
        {
            FilteredItems.Refresh();
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void UpdateFormLabels()
        {
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(SaveButtonLabel));
        }

        private void ResetFormFields()
        {
            NewName = "";
            NewCategory = "Tops";
            NewColor = "";
            NewImagePath = "";
        }

        public void OpenAddForm()
        {
            _editingItemId = null;
            ResetFormFields();
            UpdateFormLabels();
            Feedback = "";
            IsFormOpen = true;
        }

        private void OpenEditForm(ClothingItem? item)
        {
            if (item is null)
                return;

            _editingItemId = item.Id;
            NewName = item.Name;
            NewCategory = item.Category;
            NewColor = item.ColorName;
            NewImagePath = item.ImagePath ?? "";
            UpdateFormLabels();
            Feedback = "";
            IsFormOpen = true;
        }

        private void CancelForm()
        {
            IsFormOpen = false;
            _editingItemId = null;
            ResetFormFields();
            UpdateFormLabels();
            Feedback = "";
        }

        public async Task LoadAsync()
        {
            try
            {
                var data = await _service.GetAllAsync();
                Items.Clear();

                foreach (var item in data)
                    Items.Add(item);

                RefreshFilter();
                OnPropertyChanged(nameof(TotalCount));
            }
            catch (Exception ex)
            {
                Feedback = $"Failed to load clothing: {ex.Message}";
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                Feedback = "Clothing name is required.";
                return;
            }

            if (!ClothingCategories.Contains(NewCategory))
            {
                Feedback = "Please choose a valid clothing category.";
                return;
            }

            try
            {
                string? savedImagePath = string.IsNullOrWhiteSpace(NewImagePath)
                    ? null
                    : ImageStorageService.SavePhoto(NewImagePath);

                bool isEditing = _editingItemId.HasValue;

                if (isEditing)
                {
                    ClothingItem? original = Items.FirstOrDefault(x => x.Id == _editingItemId!.Value);
                    if (original is null)
                    {
                        Feedback = "Clothing item not found. Reload the wardrobe and try again.";
                        return;
                    }

                    var updated = new ClothingItem
                    {
                        Id = original.Id,
                        Name = NewName.Trim(),
                        Category = NewCategory,
                        Brand = original.Brand,
                        ColorName = NewColor.Trim(),
                        ColorHex = original.ColorHex,
                        ImagePath = savedImagePath,
                        OccasionTags = original.OccasionTags,
                        SeasonTags = original.SeasonTags,
                        Notes = original.Notes,
                        IsFavorite = original.IsFavorite,
                        CreatedAt = original.CreatedAt
                    };

                    await _service.UpdateAsync(updated);
                }
                else
                {
                    var item = new ClothingItem
                    {
                        Name = NewName.Trim(),
                        Category = NewCategory,
                        ColorName = NewColor.Trim(),
                        ImagePath = savedImagePath,
                        CreatedAt = DateTime.Now
                    };

                    await _service.AddAsync(item);
                }

                _editingItemId = null;
                IsFormOpen = false;
                ResetFormFields();
                UpdateFormLabels();
                SearchText = "";
                SelectedCategory = "All";

                await LoadAsync();
                Feedback = isEditing ? "Clothing updated successfully!" : "Clothing added successfully!";
            }
            catch (Exception ex)
            {
                Feedback = $"Failed to save clothing: {ex.Message}";
            }
        }

        public async Task<bool> DeleteAsync(ClothingItem? item)
        {
            if (item is null)
                return false;

            try
            {
                await _service.DeleteAsync(item.Id);
                Items.Remove(item);
                RefreshFilter();
                OnPropertyChanged(nameof(TotalCount));

                if (_editingItemId == item.Id)
                    CancelForm();

                Feedback = "Clothing deleted successfully!";
                return true;
            }
            catch (Exception ex)
            {
                Feedback = $"Failed to delete clothing: {ex.Message}";
                return false;
            }
        }
    }
}
