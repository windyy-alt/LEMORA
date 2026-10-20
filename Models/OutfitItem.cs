using System.ComponentModel.DataAnnotations;

namespace LEMORA.Models
{
    public class OutfitItem
    {
        public int Id { get; set; }

        public int OutfitId { get; set; }
        public int ClothingItemId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Slot { get; set; } = string.Empty;

        public Outfit Outfit { get; set; } = null!;
        public ClothingItem ClothingItem { get; set; } = null!;
    }
}
