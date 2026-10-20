
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public ObservableCollection<ClothingItem> Items { get; } = new();
        public ICollectionView FilteredItems { get; }

        public string[] Categories { get; } =
        {
            "All", "Tops", "Bottoms", "Outerwear",
            "Dresses", "Shoes", "Accessories"
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

        private string _feedback = "";
        public string Feedback
        {
            get => _feedback;
            set => SetProperty(ref _feedback, value);
        }

        public int TotalCount => Items.Count;
        public bool IsEmpty => FilteredItems.IsEmpty;

        public IRelayCommand OpenFormCommand { get; }
        public IRelayCommand CancelFormCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }

        public MyClosetViewModel()
        {
            FilteredItems = CollectionViewSource.GetDefaultView(Items);
            FilteredItems.Filter = FilterItem;

            OpenFormCommand = new RelayCommand(() =>
            {
                Feedback = "";
                IsFormOpen = true;
            });

            CancelFormCommand = new RelayCommand(() =>
            {
                IsFormOpen = false;
                Feedback = "";
            });

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

            bool matchesCategory =
                SelectedCategory == "All"
                || item.Category == SelectedCategory;

            return matchesSearch && matchesCategory;
        }

        private void RefreshFilter()
        {
            FilteredItems.Refresh();
            OnPropertyChanged(nameof(IsEmpty));
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

            try
            {
                var item = new ClothingItem
                {
                    Name = NewName.Trim(),
                    Category = NewCategory,
                    ColorName = NewColor.Trim(),
                    CreatedAt = DateTime.Now
                };

                await _service.AddAsync(item);

                NewName = "";
                NewCategory = "Tops";
                NewColor = "";
                SearchText = "";
                SelectedCategory = "All";

                await LoadAsync();

                IsFormOpen = false;
                Feedback = "Clothing added successfully!";
            }
            catch (Exception ex)
            {
                Feedback = $"Failed to save clothing: {ex.Message}";
            }
        }
    }
}
