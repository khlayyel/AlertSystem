using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExternalRecipientIdToDestinataireId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mettre à jour ExternalRecipientId avec DestinataireId pour les enregistrements existants
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL AND COL_LENGTH('Destinataire','ExternalRecipientId') IS NOT NULL
                BEGIN
                    UPDATE Destinataire 
                    SET ExternalRecipientId = CAST(DestinataireId AS NVARCHAR(50))
                    WHERE ExternalRecipientId IS NULL OR ExternalRecipientId != CAST(DestinataireId AS NVARCHAR(50));
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
