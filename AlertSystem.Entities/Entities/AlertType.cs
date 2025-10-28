namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Représente les types d'alertes disponibles dans le système
    /// </summary>
    public sealed class AlertType
    {
        /// <summary>
        /// Identifiant unique du type d'alerte
        /// </summary>
        public int AlertTypeId { get; set; }
        
        /// <summary>
        /// Nom du type d'alerte
        /// </summary>
        public string AlertTypeName { get; set; } = string.Empty;
        
        /// <summary>
        /// Description du type d'alerte
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Indique si ce type d'alerte nécessite un acquittement
        /// </summary>
        public bool RequiresAcknowledgment { get; set; }
    }
}
