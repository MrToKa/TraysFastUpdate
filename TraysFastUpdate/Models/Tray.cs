using System.ComponentModel.DataAnnotations;

namespace TraysFastUpdate.Models
{
    public class Tray
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Type { get; set; } = null!;
        [Required]
        public string Purpose { get; set; } = null!;
        [Required]
        public double Width { get; set; }
        [Required]
        public double Height { get; set; }
        [Required]
        public double Length { get; set; }
        [Required]
        public double Weight { get; set; }
    }
}
