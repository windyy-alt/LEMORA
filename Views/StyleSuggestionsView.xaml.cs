using System.Windows;
using System.Windows.Controls;
using LEMORA.ViewModels;

namespace LEMORA.Views
{
    public partial class StyleSuggestionsView : UserControl
    {
        public StyleSuggestionsViewModel ViewModel { get; } = new();

        public StyleSuggestionsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Loaded += StyleSuggestionsView_Loaded;
        }

        private async void StyleSuggestionsView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= StyleSuggestionsView_Loaded;
            await ViewModel.RefreshAsync();
        }

        public System.Threading.Tasks.Task RefreshAsync() => ViewModel.RefreshAsync();
    }
}
