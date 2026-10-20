using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LEMORA.Models
{
    public class Outfit
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Occasion { get; set; } = "Casual";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<OutfitItem> Items { get; set; } = new List<OutfitItem>();
    }
}
