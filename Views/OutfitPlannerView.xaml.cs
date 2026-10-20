using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LEMORA.ViewModels;

namespace LEMORA.Views
{
    public partial class OutfitPlannerView : UserControl
    {
        public OutfitPlannerViewModel ViewModel { get; } = new();

        public OutfitPlannerView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        public Task RefreshAsync() => ViewModel.RefreshAsync();

        private async void RemovePlan_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.HasSelectedPlan) return;

            var result = MessageBox.Show(
                $"Remove the outfit scheduled for {ViewModel.SelectedDate:MMMM d, yyyy}?",
                "Remove Schedule",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                await ViewModel.DeleteSelectedAsync();
        }
    }
}
