using System;
using System.Collections.Generic;
using System.Linq;
using LEMORA.Models;

namespace LEMORA.Services
{
    public class StyleSuggestionService
    {
        private static readonly string[] SlotOrder =
        {
            "Top", "Outerwear", "Dress", "Bottom", "Shoes", "Accessories"
        };

        public List<StyleSuggestion> Generate(
            IReadOnlyCollection<ClothingItem> wardrobe,
            string occasion,
            string colorPreference,
            int maxResults = 3)
        {
            var clothes = wardrobe.ToList();
            var tops = OfCategory(clothes, "Tops");
            var bottoms = OfCategory(clothes, "Bottoms");
            var dresses = OfCategory(clothes, "Dresses");
            var shoes = OfCategory(clothes, "Shoes");
            var outerwear = OfCategory(clothes, "Outerwear");
            var accessories = OfCategory(clothes, "Accessories");

            var bases = new List<Dictionary<string, ClothingItem>>();
            foreach (var top in tops)
            foreach (var bottom in bottoms)
            {
                if (bases.Count >= 700) break;
                bases.Add(new Dictionary<string, ClothingItem>
                {
                    ["Top"] = top, ["Bottom"] = bottom
                });
            }
            foreach (var dress in dresses.Take(100))
                bases.Add(new Dictionary<string, ClothingItem> { ["Dress"] = dress });

            var candidates = new List<(StyleSuggestion Suggestion, double Score, string BaseKey)>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var baseLook in bases)
            {
                // Use at most two options per optional slot to keep generation fast.
                var shoeOptions = OptionalOptions(shoes, occasion, colorPreference, baseLook, "Shoes");
                var layerOptions = OptionalOptions(outerwear, occasion, colorPreference, baseLook, "Outerwear");
                var accessoryOptions = OptionalOptions(accessories, occasion, colorPreference, baseLook, "Accessories");

                foreach (var shoe in shoeOptions)
                foreach (var layer in layerOptions)
                foreach (var accessory in accessoryOptions)
                {
                    var look = new Dictionary<string, ClothingItem>(baseLook);
                    if (shoe is not null) look["Shoes"] = shoe;
                    if (layer is not null) look["Outerwear"] = layer;
                    if (accessory is not null) look["Accessories"] = accessory;

                    string signature = Signature(look);
                    if (!seen.Add(signature)) continue;

                    var suggested = BuildSuggestion(look, occasion, colorPreference);
                    string baseKey = Signature(baseLook);
                    candidates.Add((suggested, ScoreLook(look, occasion, colorPreference), baseKey));
                }
            }

            var sorted = candidates
                .OrderByDescending(x => x.Score)
                .ThenBy(x => Signature(x.Suggestion.SelectedItems))
                .ToList();

            // Prefer different main garments before showing accessory variations.
            var results = new List<StyleSuggestion>();
            var usedBases = new HashSet<string>();
            var usedLooks = new HashSet<string>();

            foreach (var candidate in sorted)
            {
                if (!usedBases.Add(candidate.BaseKey)) continue;
                results.Add(candidate.Suggestion);
                usedLooks.Add(Signature(candidate.Suggestion.SelectedItems));
                if (results.Count >= maxResults) return results;
            }

            foreach (var candidate in sorted)
            {
                if (!usedLooks.Add(Signature(candidate.Suggestion.SelectedItems))) continue;
                results.Add(candidate.Suggestion);
                if (results.Count >= maxResults) break;
            }
            return results;
        }

        private static List<ClothingItem> OfCategory(IEnumerable<ClothingItem> all, string category) =>
            all.Where(item => string.Equals(item.Category?.Trim(), category,
                StringComparison.OrdinalIgnoreCase))
               .OrderBy(item => item.Id)
               .ToList();

        private static List<ClothingItem?> OptionalOptions(
            List<ClothingItem> pool, string occasion, string preference,
            Dictionary<string, ClothingItem> baseLook, string slot)
        {
            var ranked = pool
                .Where(item => !baseLook.Values.Any(existing => existing.Id == item.Id))
                .OrderByDescending(item => ScoreItem(item, occasion, preference)
                    + baseLook.Values.Sum(existing => PairScore(item, existing)))
                .ThenBy(item => item.Id)
                .Take(2)
                .Cast<ClothingItem?>()
                .ToList();

            // A look may omit a layer or accessory; shoes are preferred when present.
            if (slot != "Shoes" || ranked.Count == 0)
                ranked.Add(null);
            return ranked;
        }

        private static StyleSuggestion BuildSuggestion(
            Dictionary<string, ClothingItem> look, string occasion, string preference)
        {
            var pieces = SlotOrder
                .Where(look.ContainsKey)
                .Select(slot => new SuggestedPiece { Slot = slot, Clothing = look[slot] })
                .ToList();

            int matchedTags = pieces.Count(piece => HasOccasion(piece.Clothing, occasion));
            int matchedColors = pieces.Count(piece => MatchesPreference(piece.Clothing, preference));
            string reason = matchedTags > 0
                ? $"{matchedTags} item(s) tagged for {occasion.ToLowerInvariant()}. "
                : "Selected from your existing wardrobe. ";

            reason += preference == "All Tones"
                ? "Balanced using clothing categories and color combinations."
                : $"{matchedColors} item(s) align with your {preference.ToLowerInvariant()} palette.";

            return new StyleSuggestion
            {
                Occasion = occasion,
                ColorLabel = preference,
                Description = reason,
                SelectedItems = look.ToDictionary(entry => entry.Key, entry => entry.Value.Id),
                Pieces = pieces
            };
        }

        private static double ScoreLook(
            Dictionary<string, ClothingItem> look, string occasion, string preference)
        {
            var clothes = look.Values.ToList();
            double score = clothes.Sum(item => ScoreItem(item, occasion, preference));
            for (int i = 0; i < clothes.Count; i++)
            for (int j = i + 1; j < clothes.Count; j++)
                score += PairScore(clothes[i], clothes[j]);
            score += clothes.Count * 0.25;
            return score;
        }

        private static double ScoreItem(ClothingItem item, string occasion, string preference)
        {
            double score = HasOccasion(item, occasion) ? 12 : 0;
            if (preference != "All Tones" && MatchesPreference(item, preference))
                score += 6;
            if (item.IsFavorite) score += 1;
            return score;
        }

        private static bool HasOccasion(ClothingItem item, string occasion) =>
            (item.OccasionTags ?? "")
                .Split(new[] { ',', ';', '|', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(tag => string.Equals(tag.Trim(), occasion, StringComparison.OrdinalIgnoreCase));

        private static bool MatchesPreference(ClothingItem item, string preference)
        {
            string color = (item.ColorName ?? "").ToLowerInvariant();
            string[] words = preference switch
            {
                "Warm Neutrals" => new[] { "beige", "brown", "cream", "ivory", "tan", "khaki", "camel", "taupe", "sand", "mocha", "oat", "white" },
                "Soft Pastels" => new[] { "pink", "blush", "peach", "pastel", "lavender", "lilac", "mint", "baby blue", "sage", "soft" },
                "Classic Teal" => new[] { "teal", "turquoise", "aqua", "seafoam", "veranda", "mint", "sage" },
                _ => Array.Empty<string>()
            };
            return words.Any(word => color.Contains(word, StringComparison.OrdinalIgnoreCase));
        }

        private static double PairScore(ClothingItem first, ClothingItem second)
        {
            var a = ColorFamily(first.ColorName);
            var b = ColorFamily(second.ColorName);
            if (a == "Unknown" || b == "Unknown") return 0;
            if (a == "Neutral" || b == "Neutral") return 2;
            if (a == b) return 2;
            if ((a == "Warm" && b == "Soft") || (a == "Soft" && b == "Warm")) return 1;
            if ((a == "Cool" && b == "Soft") || (a == "Soft" && b == "Cool")) return 1;
            return 0;
        }

        private static string ColorFamily(string? name)
        {
            var value = (name ?? "").ToLowerInvariant();
            if (new[] { "white", "black", "gray", "grey", "beige", "cream", "ivory", "taupe", "khaki", "tan", "brown", "camel", "nude" }
                .Any(value.Contains)) return "Neutral";
            if (new[] { "pink", "blush", "peach", "lavender", "lilac", "pastel", "mint", "sage" }
                .Any(value.Contains)) return "Soft";
            if (new[] { "blue", "navy", "teal", "aqua", "green", "turquoise", "indigo" }
                .Any(value.Contains)) return "Cool";
            if (new[] { "red", "orange", "yellow", "maroon", "burgundy" }
                .Any(value.Contains)) return "Warm";
            return "Unknown";
        }

        private static string Signature(Dictionary<string, ClothingItem> look) =>
            string.Join("|", look.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value.Id}"));

        private static string Signature(Dictionary<string, int> look) =>
            string.Join("|", look.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"));
    }
}
