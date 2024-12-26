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
        public int? SupportsCount { get; set; }
        public double? SupportsTotalWeight { get; set; }
        public double? SupportsWeightLoadPerMeter { get; set; }
        public double? TrayWeightLoadPerMeter { get; set; }
        public double? TrayOwnWeightLoad { get; set; }
        public double? CablesWeightPerMeter { get; set; }
        public double? CablesWeightLoad { get; set; }
        public double? TotalWeightLoadPerMeter { get; set; }
        public double? TotalWeightLoad { get; set; }
    }
}
