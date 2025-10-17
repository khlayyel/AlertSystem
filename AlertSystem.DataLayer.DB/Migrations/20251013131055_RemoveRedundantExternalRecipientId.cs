using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantExternalRecipientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supprimer l'index qui dépend de ExternalRecipientId s'il existe
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Dest_Alerte_External' AND object_id = OBJECT_ID('Destinataire'))
                    DROP INDEX UX_Dest_Alerte_External ON Destinataire;
            ");

            // Supprimer la colonne ExternalRecipientId car redondante avec DestinataireId
            migrationBuilder.DropColumn(
                name: "ExternalRecipientId",
                table: "Destinataire");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalRecipientId",
                table: "Destinataire",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
