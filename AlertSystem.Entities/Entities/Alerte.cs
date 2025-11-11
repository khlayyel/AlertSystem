using System;
using System.Collections.Generic;

namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité Alerte refactorisée
    /// Chaque ligne représente une tentative d'envoi d'alerte à un destinataire spécifique via une plateforme spécifique
    /// </summary>
    public sealed class Alerte
    {
        /// <summary>
        /// ID unique de l'enregistrement d'alerte (nouvelle clé primaire)
        /// </summary>
        public long AlertRecordId { get; set; }
        
        /// <summary>
        /// ID de groupe pour regrouper les alertes liées (même alerte envoyée à plusieurs destinataires/plateformes)
        /// </summary>
        public Guid AlertGroupId { get; set; }
        
        /// <summary>
        /// ID de l'application (renommé depuis DomaineId)
        /// </summary>
        public int AppId { get; set; }
        
        /// <summary>
        /// Type d'envoi (1=Information, 2=Obligatoire) (renommé depuis TypeId)
        /// </summary>
        public int TypeEnvoieId { get; set; }
        
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
        /// Statut de l'envoi (1=En Cours, 2=Envoyée, 3=Annulée, 4=Échouée)
        /// </summary>
        public int StatutId { get; set; }
        
        /// <summary>
        /// État de lecture (1=Non lue, 2=Lue, 3=Non Confirmée, 4=Confirmée) (renommé depuis EtatAlerteId)
        /// </summary>
        public int EtatId { get; set; }
        
        /// <summary>
        /// Plateforme d'envoi (1=Email, 2=WhatsApp)
        /// </summary>
        public int PlateformeEnvoieId { get; set; }
        
        /// <summary>
        /// Destinataire (email ou numéro WhatsApp selon la plateforme)
        /// </summary>
        public string Destinataire { get; set; } = string.Empty;
        
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
        public DefApp? App { get; set; }
        public DefTypeEnvoie? TypeEnvoie { get; set; }
        public Statut? Statut { get; set; }
        public Etat? Etat { get; set; }
        public PlateformeEnvoie? PlateformeEnvoie { get; set; }
        public ICollection<RappelSuivant> Rappels { get; set; } = new List<RappelSuivant>();
    }
}
