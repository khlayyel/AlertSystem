using System;
using System.Collections.Generic;

namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité Alerte refactorisée - Fusion d'Alerte et HistoriqueAlerte
    /// Chaque ligne représente une tentative d'envoi d'alerte à un destinataire spécifique via une plateforme spécifique
    /// </summary>
    public sealed class Alerte
    {
        /// <summary>
        /// ID unique de l'enregistrement d'alerte (nouvelle clé primaire)
        /// </summary>
        public int AlertRecordId { get; set; }
        
        /// <summary>
        /// ID de groupe pour regrouper les alertes liées (même alerte envoyée à plusieurs destinataires/plateformes)
        /// </summary>
        public Guid AlertGroupId { get; set; }
        
        /// <summary>
        /// Type d'alerte (1=acquittementNécessaire, 2=acquittementNonNécessaire)
        /// </summary>
        public int AlertTypeId { get; set; }
        
        /// <summary>
        /// ID de l'application (optionnel)
        /// </summary>
        public int? AppId { get; set; }
        
        /// <summary>
        /// ID de l'expéditeur (optionnel)
        /// </summary>
        public decimal? ExpediteurId { get; set; }
        
        /// <summary>
        /// Titre de l'alerte
        /// </summary>
        public string TitreAlerte { get; set; } = string.Empty;
        
        /// <summary>
        /// Description de l'alerte
        /// </summary>
        public string? DescriptionAlerte { get; set; }
        
        /// <summary>
        /// Date de création de l'alerte
        /// </summary>
        public DateTime DateCreationAlerte { get; set; }
        
        /// <summary>
        /// Statut de l'envoi (1=En Cours, 2=Envoyé, 3=Annulé, 4=Échoué)
        /// </summary>
        public int StatutId { get; set; }
        
        /// <summary>
        /// État de lecture (1=Non lu, 2=Lu)
        /// </summary>
        public int EtatAlerteId { get; set; }
        
        /// <summary>
        /// Plateforme d'envoi (1=Email, 2=WhatsApp, 3=Desktop)
        /// </summary>
        public int PlateformeEnvoieId { get; set; }
        
        /// <summary>
        /// ID du destinataire utilisateur (pour Desktop)
        /// </summary>
        public decimal? DestinataireUserId { get; set; }
        
        /// <summary>
        /// Email du destinataire (pour Email)
        /// </summary>
        public string? DestinataireEmail { get; set; }
        
        /// <summary>
        /// Numéro de téléphone du destinataire (pour WhatsApp)
        /// </summary>
        public string? DestinatairePhoneNumber { get; set; }
        
        /// <summary>
        /// ID desktop du destinataire (pour Desktop)
        /// </summary>
        public string? DestinataireDesktop { get; set; }
        
        /// <summary>
        /// Date de lecture de l'alerte
        /// </summary>
        public DateTime? DateLecture { get; set; }
        
        /// <summary>
        /// Date du prochain rappel
        /// </summary>
        public DateTime? RappelSuivant { get; set; }
        
        /// <summary>
        /// Indique si l'alerte a été traitée par le worker
        /// </summary>
        public bool ProcessedByWorker { get; set; }
        
        /// <summary>
        /// Nombre de tentatives d'envoi (pour la logique de retry)
        /// </summary>
        public int AttemptCount { get; set; }

        // Navigation properties
        public AlertType? AlertType { get; set; }
        public Statut? Statut { get; set; }
        public Etat? Etat { get; set; }
        public DefUtilisateur? Expediteur { get; set; }
        public PlateformeEnvoie? PlateformeEnvoie { get; set; }
        public DefUtilisateur? DestinataireUser { get; set; }
        public ICollection<RappelSuivant> Rappels { get; set; } = new List<RappelSuivant>();
    }
}


