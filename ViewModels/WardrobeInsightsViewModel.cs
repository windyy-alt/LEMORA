using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LEMORA.Services;

namespace LEMORA.ViewModels
{
    public class WardrobeInsightsViewModel : ObservableObject
    {
        private readonly ClothingService _clothingService = new();
        private readonly OutfitService _outfitService = new();

        private static readonly Brush[] CategoryBrushes =
        {
            SolidBrush("#6BB1AD"), SolidBrush("#E5A9A9"),
            SolidBrush("#A7BCBD"), SolidBrush("#E6748E"),
            SolidBrush("#C4AF95"), SolidBrush("#596C73"),
            SolidBrush("#B6BA9B")
        };

        private int _totalGarments;
        public int TotalGarments
        {
            get => _totalGarments;
            private set
            {
                if (SetProperty(ref _totalGarments, value))
                {
                    OnPropertyChanged(nameof(HasWardrobe));
                    OnPropertyChanged(nameof(IsWardrobeEmpty));
                }
            }
        }

        private int _totalCategories;
        public int TotalCategories
        {
            get => _totalCategories;
            private set => SetProperty(ref _totalCategories, value);
        }

        private int _savedOutfits;
        public int SavedOutfits
        {
            get => _savedOutfits;
            private set => SetProperty(ref _savedOutfits, value);
        }

        private int _favoriteItems;
        public int FavoriteItems
        {
            get => _favoriteItems;
            private set => SetProperty(ref _favoriteItems, value);
        }

        private string _largestCategory = "—";
        public string LargestCategory
        {
            get => _largestCategory;
            private set => SetProperty(ref _largestCategory, value);
        }

        private string _largestCategoryDetail = "No collection yet";
        public string LargestCategoryDetail
        {
            get => _largestCategoryDetail;
            private set => SetProperty(ref _largestCategoryDetail, value);
        }

        private string _popularColor = "—";
        public string PopularColor
        {
            get => _popularColor;
            private set => SetProperty(ref _popularColor, value);
        }

        private string _topBottomRatio = "—";
        public string TopBottomRatio
        {
            get => _topBottomRatio;
            private set => SetProperty(ref _topBottomRatio, value);
        }

        private string _wardrobeObservation = "Add clothing to see your collection overview.";
        public string WardrobeObservation
        {
            get => _wardrobeObservation;
            private set => SetProperty(ref _wardrobeObservation, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool HasWardrobe => TotalGarments > 0;
        public bool IsWardrobeEmpty => !HasWardrobe;

        public ObservableCollection<WardrobeCategoryInsight> Categories { get; } = new();
        public ObservableCollection<WardrobeColorInsight> Colors { get; } = new();

        public async Task RefreshAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            ErrorMessage = "";

            try
            {
                var clothes = await _clothingService.GetAllAsync();
                var outfitCount = await _outfitService.GetCountAsync();
                int total = clothes.Count;

                var categoryGroups = clothes
                    .GroupBy(item => string.IsNullOrWhiteSpace(item.Category)
                        ? "Uncategorized" : item.Category.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new { Name = group.Key, Count = group.Count() })
                    .OrderByDescending(group => group.Count)
                    .ThenBy(group => group.Name)
                    .ToList();

                Categories.Clear();
                double angle = -90;
                for (int i = 0; i < categoryGroups.Count; i++)
                {
                    var group = categoryGroups[i];
                    double percent = 100.0 * group.Count / total;
                    double sweep = 360.0 * group.Count / total;

                    Categories.Add(new WardrobeCategoryInsight
                    {
                        Name = group.Name,
                        Count = group.Count,
                        Percentage = percent,
                        Swatch = CategoryBrushes[i % CategoryBrushes.Length],
                        DonutGeometry = BuildDonutSegment(angle, sweep)
                    });
                    angle += sweep;
                }

                var colorGroups = clothes
                    .GroupBy(item => string.IsNullOrWhiteSpace(item.ColorName)
                        ? "Unspecified" : item.ColorName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new { Name = group.Key, Count = group.Count() })
                    .OrderByDescending(group => group.Count)
                    .ThenBy(group => group.Name)
                    .ToList();

                Colors.Clear();
                foreach (var group in colorGroups.Take(6))
                {
                    Colors.Add(new WardrobeColorInsight
                    {
                        Name = group.Name,
                        Count = group.Count,
                        Percentage = 100.0 * group.Count / total,
                        Swatch = ColorBrush(group.Name)
                    });
                }

                TotalGarments = total;
                TotalCategories = categoryGroups.Count;
                SavedOutfits = outfitCount;
                FavoriteItems = clothes.Count(item => item.IsFavorite);

                var largest = categoryGroups.FirstOrDefault();
                LargestCategory = largest?.Name ?? "—";
                LargestCategoryDetail = largest is null
                    ? "No collection yet"
                    : $"{largest.Count} of {total} items ({largest.Count * 100.0 / total:0}%)";
                PopularColor = colorGroups.FirstOrDefault()?.Name ?? "—";

                int tops = clothes.Count(item => string.Equals(item.Category, "Tops", StringComparison.OrdinalIgnoreCase));
                int bottoms = clothes.Count(item => string.Equals(item.Category, "Bottoms", StringComparison.OrdinalIgnoreCase));
                TopBottomRatio = bottoms > 0 ? $"{(double)tops / bottoms:0.0}:1" : "—";

                WardrobeObservation = total == 0
                    ? "Add clothing to see your collection overview."
                    : largest is not null
                        ? $"{largest.Name} is your largest category, making up {largest.Count * 100.0 / total:0}% of the collection. This overview uses the item details you saved in My Closet."
                        : "Add clothing to see your collection overview.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unable to load wardrobe insights: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static Brush SolidBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private static Brush ColorBrush(string name)
        {
            string key = name.Trim().ToLowerInvariant();
            if (key.StartsWith('#'))
            {
                try { return SolidBrush(key); }
                catch (FormatException) { }
            }

            if (key.Contains("white") || key.Contains("ivory")) return SolidBrush("#F6F4E9");
            if (key.Contains("cream") || key.Contains("beige")) return SolidBrush("#DDD1B5");
            if (key.Contains("black") || key.Contains("charcoal")) return SolidBrush("#343A40");
            if (key.Contains("gray") || key.Contains("grey") || key.Contains("silver")) return SolidBrush("#A7BCBD");
            if (key.Contains("brown") || key.Contains("camel") || key.Contains("tan")) return SolidBrush("#A47B5B");
            if (key.Contains("pink") || key.Contains("rose")) return SolidBrush("#E6748E");
            if (key.Contains("red") || key.Contains("burgundy")) return SolidBrush("#B65B61");
            if (key.Contains("blue") || key.Contains("navy") || key.Contains("denim")) return SolidBrush("#557D97");
            if (key.Contains("green") || key.Contains("olive") || key.Contains("sage")) return SolidBrush("#819C80");
            if (key.Contains("yellow") || key.Contains("gold")) return SolidBrush("#D8B86A");
            if (key.Contains("purple") || key.Contains("lavender") || key.Contains("lilac")) return SolidBrush("#AD94BA");
            if (key.Contains("orange") || key.Contains("coral")) return SolidBrush("#DA9270");
            return SolidBrush("#A7BCBD");
        }

        private static Geometry BuildDonutSegment(double startAngle, double sweep)
        {
            const double cx = 100;
            const double cy = 100;
            const double outer = 89;
            const double inner = 57;

            if (sweep <= 0)
                return Geometry.Empty;

            // Full circles use two arcs so the ring closes correctly.
            if (sweep >= 359.999)
            {
                var full = new StreamGeometry();
                using (var context = full.Open())
                {
                    Point startOuter = PointOnCircle(startAngle, outer);
                    Point midOuter = PointOnCircle(startAngle + 180, outer);
                    Point startInner = PointOnCircle(startAngle, inner);
                    Point midInner = PointOnCircle(startAngle + 180, inner);

                    context.BeginFigure(startOuter, true, true);
                    context.ArcTo(midOuter, new Size(outer, outer), 0, false,
                        SweepDirection.Clockwise, true, false);
                    context.ArcTo(startOuter, new Size(outer, outer), 0, false,
                        SweepDirection.Clockwise, true, false);
                    context.LineTo(startInner, true, false);
                    context.ArcTo(midInner, new Size(inner, inner), 0, false,
                        SweepDirection.Counterclockwise, true, false);
                    context.ArcTo(startInner, new Size(inner, inner), 0, false,
                        SweepDirection.Counterclockwise, true, false);
                }
                full.Freeze();
                return full;
            }

            Point PointOnCircle(double angle, double radius)
            {
                double radians = angle * Math.PI / 180.0;
                return new Point(cx + radius * Math.Cos(radians), cy + radius * Math.Sin(radians));
            }

            var geo = new StreamGeometry();
            using (var context = geo.Open())
            {
                Point outerStart = PointOnCircle(startAngle, outer);
                Point outerEnd = PointOnCircle(startAngle + sweep, outer);
                Point innerEnd = PointOnCircle(startAngle + sweep, inner);
                Point innerStart = PointOnCircle(startAngle, inner);
                bool largeArc = sweep > 180;

                context.BeginFigure(outerStart, true, true);
                context.ArcTo(outerEnd, new Size(outer, outer), 0, largeArc,
                    SweepDirection.Clockwise, true, false);
                context.LineTo(innerEnd, true, false);
                context.ArcTo(innerStart, new Size(inner, inner), 0, largeArc,
                    SweepDirection.Counterclockwise, true, false);
            }
            geo.Freeze();
            return geo;
        }
    }

    public class WardrobeCategoryInsight
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public Brush Swatch { get; set; } = Brushes.Gray;
        public Geometry DonutGeometry { get; set; } = Geometry.Empty;
        public string Summary => $"{Count} items  ·  {Percentage:0}%";
    }

    public class WardrobeColorInsight
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public Brush Swatch { get; set; } = Brushes.Gray;
        public string Summary => $"{Count} items ({Percentage:0}%)";
    }
}
