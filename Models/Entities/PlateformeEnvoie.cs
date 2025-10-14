using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertSystem.Models.Entities;

[Table("PlateformeEnvoie")]
public sealed class PlateformeEnvoie
{
    [Key]
    public int PlateformeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Plateforme { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Alerte> Alertes { get; set; } = new List<Alerte>();
}
