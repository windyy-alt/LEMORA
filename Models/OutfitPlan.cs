using System;
using System.ComponentModel.DataAnnotations;

namespace LEMORA.Models
{
    public class OutfitPlan
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public int OutfitId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Activity { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Notes { get; set; }

        public Outfit Outfit { get; set; } = null!;
    }
}
