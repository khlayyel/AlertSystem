using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Utils.Database
{
    /// <summary>
    /// Centralized repository include patterns to eliminate duplication across projects
    /// Note: This class provides extension method patterns that should be implemented
    /// in the consuming projects to avoid circular dependencies
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Example pattern for applying includes to Alerte entities
        /// This should be implemented in the consuming project with actual entity types
        /// </summary>
        public static class AlerteIncludePatterns
        {
            public const string BasicIncludes = "AlertType,Statut,Etat,ExpedType,Expediteur,PlateformeEnvoie,Destinataire";
            public const string AllIncludes = "AlertType,Statut,Etat,ExpedType,Expediteur,PlateformeEnvoie,Destinataire,HistoriqueAlertes.PlateformeEnvoie";
            public const string HistoriqueIncludes = "Alerte.AlertType,Alerte.Statut,Alerte.Etat,Alerte.ExpedType,Alerte.Expediteur,PlateformeEnvoie";
        }

        /// <summary>
        /// Example pattern for applying includes to User entities
        /// </summary>
        public static class UserIncludePatterns
        {
            public const string BasicIncludes = "Department";
        }

        /// <summary>
        /// Example pattern for applying includes to other entities
        /// </summary>
        public static class EntityIncludePatterns
        {
            public const string AlertTypeIncludes = ""; // No navigation properties
            public const string PlateformeEnvoieIncludes = ""; // No navigation properties
            public const string StatutIncludes = ""; // No navigation properties
            public const string EtatIncludes = ""; // No navigation properties
            public const string ExpedTypeIncludes = ""; // No navigation properties
        }
    }
}
