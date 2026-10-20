using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LEMORA.Models;
using LEMORA.ViewModels;

namespace LEMORA.Views
{
    public partial class MyClosetView : UserControl
    {
        public MyClosetViewModel ViewModel { get; } = new();

        public MyClosetView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Loaded += MyClosetView_Loaded;
        }

        private async void MyClosetView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MyClosetView_Loaded;
            await ViewModel.LoadAsync();
        }

        public void OpenAddForm()
        {
            ViewModel.OpenAddForm();
        }

        private void BrowsePhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Clothing Photo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
                ViewModel.NewImagePath = dialog.FileName;
        }

        private async void DeleteClothing_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ClothingItem item)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{item.Name}'?",
                "Delete Clothing",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                await ViewModel.DeleteAsync(item);
        }
    }
}
