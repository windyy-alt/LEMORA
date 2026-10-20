using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEMORA.Models;
using LEMORA.Services;

namespace LEMORA.ViewModels
{
    public class StyleSuggestionsViewModel : ObservableObject
    {
        private readonly ClothingService _clothingService = new();
        private readonly OutfitService _outfitService = new();
        private readonly StyleSuggestionService _suggestionService = new();

        public string[] Occasions { get; } = new[]
            { "Casual", "Campus", "Formal", "Hangout", "Dinner" };

        public string[] ColorPreferences { get; } = new[]
            { "All Tones", "Warm Neutrals", "Soft Pastels", "Classic Teal" };

        public ObservableCollection<StyleSuggestion> Suggestions { get; } = new();

        private string _selectedOccasion = "Casual";
        public string SelectedOccasion
        {
            get => _selectedOccasion;
            set => SetProperty(ref _selectedOccasion, value);
        }

        private string _selectedColorPreference = "All Tones";
        public string SelectedColorPreference
        {
            get => _selectedColorPreference;
            set => SetProperty(ref _selectedColorPreference, value);
        }

        private int _wardrobeCount;
        public int WardrobeCount
        {
            get => _wardrobeCount;
            private set => SetProperty(ref _wardrobeCount, value);
        }

        private string _feedback = "Choose an occasion and click Generate Suggestions.";
        public string Feedback
        {
            get => _feedback;
            private set => SetProperty(ref _feedback, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool HasSuggestions => Suggestions.Count > 0;
        public bool HasNoSuggestions => !HasSuggestions;
        public string ResultSummary => $"{Suggestions.Count} look(s) generated";

        public IAsyncRelayCommand GenerateCommand { get; }
        public IAsyncRelayCommand<StyleSuggestion> SaveSuggestionCommand { get; }

        public StyleSuggestionsViewModel()
        {
            GenerateCommand = new AsyncRelayCommand(GenerateAsync);
            SaveSuggestionCommand = new AsyncRelayCommand<StyleSuggestion>(SaveSuggestionAsync);
        }

        public async Task RefreshAsync()
        {
            try
            {
                WardrobeCount = await _clothingService.GetCountAsync();
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to read wardrobe: {ex.Message}";
            }
        }

        private async Task GenerateAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var clothes = await _clothingService.GetAllAsync();
                WardrobeCount = clothes.Count;
                var generated = _suggestionService.Generate(
                    clothes, SelectedOccasion, SelectedColorPreference);
                var savedOutfits = await _outfitService.GetAllAsync();

                Suggestions.Clear();
                for (int index = 0; index < generated.Count; index++)
                {
                    var suggestion = generated[index];
                    suggestion.Title = $"{SelectedOccasion} Look {index + 1:00}";
                    suggestion.IsSaved = savedOutfits.Any(outfit =>
                        string.Equals(outfit.Occasion, suggestion.Occasion,
                            StringComparison.OrdinalIgnoreCase) &&
                        SameCombination(outfit, suggestion.SelectedItems));
                    Suggestions.Add(suggestion);
                }

                NotifyResultChanged();
                Feedback = generated.Count == 0
                    ? "Add at least one dress, or a top and a bottom, in My Closet to generate an outfit."
                    : $"Found {generated.Count} look(s) from your wardrobe. Suggestions use category, occasion tags, and simple color rules.";
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to generate suggestions: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveSuggestionAsync(StyleSuggestion? suggestion)
        {
            if (suggestion is null || suggestion.IsSaved || IsLoading) return;
            IsLoading = true;
            try
            {
                var existing = await _outfitService.GetAllAsync();
                if (existing.Any(outfit =>
                    string.Equals(outfit.Occasion, suggestion.Occasion,
                        StringComparison.OrdinalIgnoreCase) &&
                    SameCombination(outfit, suggestion.SelectedItems)))
                {
                    suggestion.IsSaved = true;
                    Feedback = "This combination is already saved in Outfit Studio.";
                    return;
                }

                await _outfitService.SaveAsync(
                    null, suggestion.Title, suggestion.Occasion, suggestion.SelectedItems);
                suggestion.IsSaved = true;
                Feedback = $"'{suggestion.Title}' saved! Open Outfit Studio to edit or schedule it.";
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to save outfit: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NotifyResultChanged()
        {
            OnPropertyChanged(nameof(HasSuggestions));
            OnPropertyChanged(nameof(HasNoSuggestions));
            OnPropertyChanged(nameof(ResultSummary));
        }

        private static bool SameCombination(
            Outfit existing, IReadOnlyDictionary<string, int> selected)
        {
            if (existing.Items.Count != selected.Count) return false;
            return existing.Items.All(piece =>
                selected.TryGetValue(piece.Slot, out var id) && id == piece.ClothingItemId);
        }
    }
}
