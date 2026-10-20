using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LEMORA.ViewModels;

namespace LEMORA.Views
{
    public partial class WardrobeInsightsView : UserControl
    {
        public WardrobeInsightsViewModel ViewModel { get; } = new();

        public WardrobeInsightsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        public Task RefreshAsync() => ViewModel.RefreshAsync();

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }
    }
}
