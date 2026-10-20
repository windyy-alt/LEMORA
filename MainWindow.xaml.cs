using System;
using System.Windows;
using System.Windows.Media;
using LEMORA.ViewModels;

namespace LEMORA
{
    public partial class MainWindow : Window
    {
        private readonly DashboardViewModel _dashboardViewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DashboardPanel.DataContext = _dashboardViewModel;
            UpdateGreeting();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;
            await _dashboardViewModel.RefreshAsync();
        }

        private void UpdateGreeting()
        {
            int hour = DateTime.Now.Hour;
            string greeting = hour switch
            {
                >= 5 and < 11 => "Good morning",
                >= 11 and < 18 => "Good afternoon",
                _ => "Good evening"
            };

            GreetingText.Text = $"{greeting}, welcome to Lemora!";
        }

        private async void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            UpdateGreeting();
            DashboardPanel.Visibility = Visibility.Visible;
            ClosetPanel.Visibility = Visibility.Collapsed;
            DashboardButton.Background = (Brush)FindResource("PrimaryBrush");
            MyClosetButton.Background = Brushes.Transparent;
            await _dashboardViewModel.RefreshAsync();
        }

        private void MyCloset_Click(object sender, RoutedEventArgs e)
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            ClosetPanel.Visibility = Visibility.Visible;
            DashboardButton.Background = Brushes.Transparent;
            MyClosetButton.Background = (Brush)FindResource("PrimaryBrush");
        }

        private void AddClothing_Click(object sender, RoutedEventArgs e)
        {
            MyCloset_Click(sender, e);
            ClosetPanel.OpenAddForm();
        }
    }
}
