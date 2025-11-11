namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table def_Utilisateur (nouvelle table pour les utilisateurs du système d'alertes)
    /// </summary>
    public sealed class DefUtilisateur
    {
        public int UtilisateurId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int AppId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? WhatsAppNumber { get; set; }
        public DefApp? App { get; set; }
    }
}
