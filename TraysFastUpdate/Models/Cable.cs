using System.ComponentModel.DataAnnotations;

namespace TraysFastUpdate.Models
{
    public class Cable
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Tag { get; set; } = null!;
        public int CableTypeId { get; set; }
        public CableType CableType { get; set; } = null!;
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? Routing { get; set; }
    }
}
