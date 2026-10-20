using System;
using System.Threading.Tasks;
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

        private void ShowPage(UIElement panel)
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            ClosetPanel.Visibility = Visibility.Collapsed;
            OutfitStudioPanel.Visibility = Visibility.Collapsed;
            OutfitPlannerPanel.Visibility = Visibility.Collapsed;
            StyleSuggestionsPanel.Visibility = Visibility.Collapsed;

            DashboardButton.Background = Brushes.Transparent;
            MyClosetButton.Background = Brushes.Transparent;
            OutfitStudioButton.Background = Brushes.Transparent;
            OutfitPlannerButton.Background = Brushes.Transparent;
            StyleSuggestionsButton.Background = Brushes.Transparent;

            panel.Visibility = Visibility.Visible;
        }

        private async void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(DashboardPanel);
            DashboardButton.Background = (Brush)FindResource("PrimaryBrush");
            UpdateGreeting();
            await _dashboardViewModel.RefreshAsync();
        }

        private void MyCloset_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(ClosetPanel);
            MyClosetButton.Background = (Brush)FindResource("PrimaryBrush");
        }

        private async Task ShowOutfitStudioAsync()
        {
            ShowPage(OutfitStudioPanel);
            OutfitStudioButton.Background = (Brush)FindResource("PrimaryBrush");
            await OutfitStudioPanel.RefreshAsync();
        }

        private async void OutfitStudio_Click(object sender, RoutedEventArgs e)
        {
            await ShowOutfitStudioAsync();
        }

        private async void CreateOutfit_Click(object sender, RoutedEventArgs e)
        {
            await ShowOutfitStudioAsync();
            OutfitStudioPanel.BeginNewOutfit();
        }

        private async void OutfitPlanner_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(OutfitPlannerPanel);
            OutfitPlannerButton.Background = (Brush)FindResource("PrimaryBrush");
            await OutfitPlannerPanel.RefreshAsync();
        }

        private async void StyleSuggestions_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(StyleSuggestionsPanel);
            StyleSuggestionsButton.Background = (Brush)FindResource("PrimaryBrush");
            await StyleSuggestionsPanel.RefreshAsync();
        }

        private void AddClothing_Click(object sender, RoutedEventArgs e)
        {
            MyCloset_Click(sender, e);
            ClosetPanel.OpenAddForm();
        }
    }
}
