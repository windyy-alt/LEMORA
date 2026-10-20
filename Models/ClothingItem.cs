
using System;
using System.ComponentModel.DataAnnotations;

namespace LEMORA.Models
{
    public class ClothingItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(50)]
        public string ColorName { get; set; } = string.Empty;

        public string ColorHex { get; set; } = "#A7BCBD";

        public string? ImagePath { get; set; }

        public string OccasionTags { get; set; } = string.Empty;

        public string SeasonTags { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public bool IsFavorite { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
