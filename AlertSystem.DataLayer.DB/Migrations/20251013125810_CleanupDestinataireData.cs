using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class CleanupDestinataireData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nettoyer les duplications dans la table Destinataire
            migrationBuilder.Sql(@"
                -- Supprimer les doublons en gardant seulement le premier enregistrement pour chaque AlerteId
                WITH CTE AS (
                    SELECT DestinataireId, 
                           ROW_NUMBER() OVER (PARTITION BY AlerteId ORDER BY DestinataireId) as rn
                    FROM Destinataire
                )
                DELETE FROM CTE WHERE rn > 1;
            ");

            // Mettre à jour ExternalRecipientId avec DestinataireId
            migrationBuilder.Sql(@"
                UPDATE Destinataire 
                SET ExternalRecipientId = CAST(DestinataireId AS NVARCHAR(50));
            ");

            // Ajouter une contrainte unique pour éviter les futures duplications
            migrationBuilder.CreateIndex(
                name: "IX_Destinataire_AlerteId_Unique",
                table: "Destinataire",
                column: "AlerteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer la contrainte unique ajoutée
            migrationBuilder.DropIndex(
                name: "IX_Destinataire_AlerteId_Unique",
                table: "Destinataire");
        }
    }
}
