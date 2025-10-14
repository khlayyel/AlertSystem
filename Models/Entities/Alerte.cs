using System;
using System.Collections.Generic;

namespace AlertSystem.Models.Entities
{
    public sealed class Alerte
    {
        public int AlerteId { get; set; }
        public int AlertTypeId { get; set; }
        public int? AppId { get; set; }
        public int ExpedTypeId { get; set; }
        public int? ExpediteurId { get; set; }
        public string TitreAlerte { get; set; } = string.Empty;
        public string? DescriptionAlerte { get; set; }
        public DateTime DateCreationAlerte { get; set; }
        public int StatutId { get; set; }
        public int EtatAlerteId { get; set; }
        
        // Nouvelles colonnes ajoutées
        public int? PlateformeEnvoieId { get; set; }  // Clé étrangère vers PlateformeEnvoie
        public int? DestinataireId { get; set; }      // Clé étrangère vers Users

        public AlertType? AlertType { get; set; }
        public ExpedType? ExpedType { get; set; }
        public Statut? Statut { get; set; }
        public Etat? Etat { get; set; }
        
        // Navigation properties pour les nouvelles colonnes
        public PlateformeEnvoie? PlateformeEnvoie { get; set; }
        public User? Destinataire { get; set; }

        public ICollection<HistoriqueAlerte> HistoriqueAlertes { get; set; } = new List<HistoriqueAlerte>();
        public ICollection<RappelSuivant> Rappels { get; set; } = new List<RappelSuivant>();
    }
}


