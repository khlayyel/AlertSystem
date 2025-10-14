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
                UPDATE Destinataire 
                SET ExternalRecipientId = CAST(DestinataireId AS NVARCHAR(50))
                WHERE ExternalRecipientId IS NULL OR ExternalRecipientId != CAST(DestinataireId AS NVARCHAR(50));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
