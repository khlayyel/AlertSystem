using System.Collections.Generic;

namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_TypeAlerte
    /// </summary>
    public sealed class DefTypeAlerte
    {
        public int TypeAlertId { get; set; }
        public int AppId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DefApp? App { get; set; }
        public ICollection<DefAlerte> Alertes { get; set; } = new List<DefAlerte>();
    }
}

