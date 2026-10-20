using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEMORA.Models
{
    public class SuggestedPiece
    {
        public string Slot { get; init; } = "";
        public ClothingItem Clothing { get; init; } = null!;
    }

    public class StyleSuggestion : ObservableObject
    {
        private bool _isSaved;

        public string Title { get; set; } = "";
        public string Occasion { get; init; } = "";
        public string Description { get; init; } = "";
        public string ColorLabel { get; init; } = "";
        public List<SuggestedPiece> Pieces { get; init; } = new();
        public Dictionary<string, int> SelectedItems { get; init; } = new();
        public int ItemCount => Pieces.Count;
        public string ItemSummary => $"{ItemCount} wardrobe pieces";
        public bool CanSave => !IsSaved;

        public bool IsSaved
        {
            get => _isSaved;
            set
            {
                if (SetProperty(ref _isSaved, value))
                    OnPropertyChanged(nameof(CanSave));
            }
        }
    }
}
