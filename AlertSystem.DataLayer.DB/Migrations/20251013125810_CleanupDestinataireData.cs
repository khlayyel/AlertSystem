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
                IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL
                BEGIN
                    WITH CTE AS (
                        SELECT DestinataireId, 
                               ROW_NUMBER() OVER (PARTITION BY AlerteId ORDER BY DestinataireId) as rn
                        FROM Destinataire
                    )
                    DELETE FROM CTE WHERE rn > 1;
                END
            ");

            // Mettre à jour ExternalRecipientId avec DestinataireId
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL AND COL_LENGTH('Destinataire','ExternalRecipientId') IS NOT NULL
                BEGIN
                    UPDATE Destinataire 
                    SET ExternalRecipientId = CAST(DestinataireId AS NVARCHAR(50));
                END
            ");

            // Ajouter une contrainte unique pour éviter les futures duplications
            if (migrationBuilder != null)
            {
                migrationBuilder.Sql(@"
                    IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL AND NOT EXISTS (
                        SELECT 1 FROM sys.indexes WHERE name = 'IX_Destinataire_AlerteId_Unique' AND object_id = OBJECT_ID('Destinataire')
                    )
                    BEGIN
                        CREATE UNIQUE INDEX IX_Destinataire_AlerteId_Unique ON Destinataire(AlerteId);
                    END
                ");
            }
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
