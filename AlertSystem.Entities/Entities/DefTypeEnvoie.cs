namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_TypeEnvoie (renommée depuis def_Type)
    /// </summary>
    public sealed class DefTypeEnvoie
    {
        public int TypeEnvoieId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

