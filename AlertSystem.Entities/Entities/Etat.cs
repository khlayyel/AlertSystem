namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_Etat
    /// </summary>
    public sealed class Etat
    {
        public int EtatId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
