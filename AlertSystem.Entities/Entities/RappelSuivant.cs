using System;

namespace AlertSystem.Entities.Entities
{
    public sealed class RappelSuivant
    {
        public int RappelId { get; set; }
        // FK to Alerte.AlertRecordId (bigint)
        public long AlerteId { get; set; }
        public DateTime DateRappel { get; set; }
        public string? StatutRappel { get; set; }
        public int Tentative { get; set; }
        public string? DetailsErreur { get; set; }

        public Alerte? Alerte { get; set; }
    }
}
