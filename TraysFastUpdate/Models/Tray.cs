using System.ComponentModel.DataAnnotations;

namespace TraysFastUpdate.Models
{
    public class Tray
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public double Width { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
        public double Weight { get; set; }
    }
}
