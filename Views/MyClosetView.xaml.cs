
using System.Windows;
using System.Windows.Controls;
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
            ViewModel.IsFormOpen = true;
        }
    }
}
