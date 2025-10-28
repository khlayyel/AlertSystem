using System;

namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Table de queue pour la communication entre WatcherWorker et SenderWorker
    /// </summary>
    public sealed class AlertProcessingQueue
    {
        /// <summary>
        /// ID unique de l'élément de queue
        /// </summary>
        public int QueueId { get; set; }
        
        /// <summary>
        /// ID de l'enregistrement d'alerte à traiter
        /// </summary>
        public int AlertRecordId { get; set; }
        
        /// <summary>
        /// Date d'ajout à la queue
        /// </summary>
        public DateTime QueuedAt { get; set; }
        
        /// <summary>
        /// Priorité de traitement (plus bas = plus prioritaire)
        /// </summary>
        public int Priority { get; set; } = 0;
        
        /// <summary>
        /// Nombre de tentatives de traitement
        /// </summary>
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// Date de la dernière tentative
        /// </summary>
        public DateTime? LastAttemptAt { get; set; }
        
        /// <summary>
        /// Message d'erreur de la dernière tentative
        /// </summary>
        public string? LastError { get; set; }
    }
}
