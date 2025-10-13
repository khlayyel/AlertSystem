namespace AlertSystem.Models.Entities
{
    public sealed class AlertType
    {
        public int AlertTypeId { get; set; }
        public string AlertTypeName { get; set; } = string.Empty; // maps to AlertType
    }

    public sealed class ExpedType
    {
        public int ExpedTypeId { get; set; }
        public string ExpedTypeName { get; set; } = string.Empty; // maps to ExpedType
    }

    public sealed class Statut
    {
        public int StatutId { get; set; }
        public string StatutName { get; set; } = string.Empty; // maps to Statut
    }

    public sealed class Etat
    {
        public int EtatAlerteId { get; set; }
        public string EtatAlerteName { get; set; } = string.Empty; // maps to EtatAlerte
    }

    public sealed class Destinataire
    {
        public int DestinataireId { get; set; }
        public int AlerteId { get; set; }
        public string? EtatAlerte { get; set; }
        public System.DateTime? DateLecture { get; set; }
        public string? ExternalRecipientId { get; set; }

        public Alerte? Alerte { get; set; }
    }

    public sealed class RappelSuivant
    {
        public int RappelId { get; set; }
        public int AlerteId { get; set; }
        public System.DateTime DateRappel { get; set; }
        public string? StatutRappel { get; set; }
        public int Tentative { get; set; }

        public Alerte? Alerte { get; set; }
    }
}


