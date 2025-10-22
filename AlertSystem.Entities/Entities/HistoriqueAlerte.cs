using System;
using AlertSystem.Entities.Entities;

namespace AlertSystem.Entities.Entities
{
    public sealed class HistoriqueAlerte
    {
        public int HistoriqueAlerteId { get; set; }
        public int DestinataireId { get; set; }
        public int AlerteId { get; set; }
        public int? DestinataireUserId { get; set; }
        public int EtatAlerteId { get; set; }
        public DateTime? DateLecture { get; set; }
        public DateTime? RappelSuivant { get; set; }
        public string? DestinataireEmail { get; set; }
        public string? DestinatairePhoneNumber { get; set; }
        public string? DestinataireDesktop { get; set; }
        public int? PlateformeEnvoieId { get; set; }

        public Alerte? Alerte { get; set; }
        public User? User { get; set; }
        public Etat? Etat { get; set; }
        public PlateformeEnvoie? PlateformeEnvoie { get; set; }
    }
}
