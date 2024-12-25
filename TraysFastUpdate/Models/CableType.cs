using System.ComponentModel.DataAnnotations;

namespace TraysFastUpdate.Models
{
    public class CableType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Type { get; set; } = null!;
        [Required]
        public string Purpose { get; set; } = null!;
        [Required]
        public double Diameter { get; set; }
        [Required]
        public double Weight { get; set; }
        public ICollection<Cable> Cables { get; set; } = new List<Cable>();
    }
}
