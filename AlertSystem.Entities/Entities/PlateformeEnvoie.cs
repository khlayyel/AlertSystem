using System.Collections.Generic;

namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_PlateformeEnvoi
    /// </summary>
    public sealed class PlateformeEnvoie
    {
        public int PlateformeId { get; set; }
        public string Description { get; set; } = string.Empty;
        public ICollection<Alerte> Alertes { get; set; } = new List<Alerte>();
    }
}
