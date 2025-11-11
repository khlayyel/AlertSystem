using System.Collections.Generic;

namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_App (renommée depuis def_Domaine)
    /// </summary>
    public sealed class DefApp
    {
        public int AppId { get; set; }
        public string Description { get; set; } = string.Empty;
        public ICollection<DefTypeAlerte> TypeAlertes { get; set; } = new List<DefTypeAlerte>();
        public ICollection<DefUtilisateur> Utilisateurs { get; set; } = new List<DefUtilisateur>();
    }
}

