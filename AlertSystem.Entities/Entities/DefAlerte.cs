namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_Alerte (définition des alertes à poller)
    /// </summary>
    public sealed class DefAlerte
    {
        public int DefAlerteId { get; set; }
        public int DefTypeAlerte { get; set; }
        public string ListDestinatairesId { get; set; } = string.Empty; // JSON array: [1,2,3]
        public string URL { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DefTypeAlerte? TypeAlerte { get; set; }
    }
}

