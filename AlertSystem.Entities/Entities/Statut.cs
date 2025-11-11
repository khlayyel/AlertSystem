namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_Statut
    /// </summary>
    public sealed class Statut
    {
        public int StatutId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
