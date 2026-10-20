using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LEMORA.Models;
using LEMORA.ViewModels;

namespace LEMORA.Views
{
    public partial class OutfitStudioView : UserControl
    {
        public OutfitStudioViewModel ViewModel { get; } = new();

        public OutfitStudioView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Loaded += OutfitStudioView_Loaded;
        }

        private async void OutfitStudioView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OutfitStudioView_Loaded;
            await ViewModel.RefreshAsync();
        }

        public Task RefreshAsync() => ViewModel.RefreshAsync();

        public void BeginNewOutfit() => ViewModel.BeginNewOutfit();

        private async void DeleteSavedOutfit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not Outfit outfit)
                return;

            var answer = MessageBox.Show(
                $"Delete '{outfit.Name}'? This cannot be undone.",
                "Delete Outfit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Yes)
                await ViewModel.DeleteOutfitAsync(outfit);
        }
    }
}
