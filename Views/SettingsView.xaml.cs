using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LEMORA.Data;
using LEMORA.Services;
using Microsoft.EntityFrameworkCore;

namespace LEMORA.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        public async Task RefreshAsync()
        {
            ThemeStatusText.Text = ThemeService.IsDark
                ? "Active theme: Charcoal Velvet (Dark)"
                : "Active theme: Pastel Atelier (Light)";

            DatabasePathText.Text = LemoraDbContext.DatabasePath;
            try
            {
                await using var db = new LemoraDbContext();
                ClothingCountText.Text = (await db.ClothingItems.CountAsync()).ToString();
                OutfitCountText.Text = (await db.Outfits.CountAsync()).ToString();

                var file = new FileInfo(LemoraDbContext.DatabasePath);
                StorageInfoText.Text = file.Exists
                    ? $"Database size: {file.Length / 1024d / 1024d:0.00} MB"
                    : "The database file has not been created yet.";
            }
            catch (Exception ex)
            {
                ClothingCountText.Text = "—";
                OutfitCountText.Text = "—";
                StorageInfoText.Text = $"Could not read database information: {ex.Message}";
            }
        }

        private async void LightMode_Click(object sender, RoutedEventArgs e)
        {
            await ChangeThemeAsync(false);
        }

        private async void DarkMode_Click(object sender, RoutedEventArgs e)
        {
            await ChangeThemeAsync(true);
        }

        private async Task ChangeThemeAsync(bool dark)
        {
            try
            {
                ThemeService.ApplyTheme(dark);
                FeedbackText.Text = "Theme preference saved.";
            }
            catch (Exception ex)
            {
                FeedbackText.Text = $"Theme changed, but saving the preference failed: {ex.Message}";
            }
            await RefreshAsync();
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.GetDirectoryName(LemoraDbContext.DatabasePath)!;
            try
            {
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                FeedbackText.Text = $"Could not open data folder: {ex.Message}";
            }
        }
    }
}
