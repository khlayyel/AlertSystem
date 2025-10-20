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
                IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Dest_Alerte_External' AND object_id = OBJECT_ID('Destinataire'))
                    DROP INDEX UX_Dest_Alerte_External ON Destinataire;
            ");

            // Supprimer la colonne ExternalRecipientId car redondante avec DestinataireId
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL AND COL_LENGTH('Destinataire','ExternalRecipientId') IS NOT NULL
                BEGIN
                    DECLARE @var sysname;
                    SELECT @var = [d].[name]
                    FROM [sys].[default_constraints] [d]
                    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Destinataire]') AND [c].[name] = N'ExternalRecipientId');
                    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Destinataire] DROP CONSTRAINT [' + @var + ']');
                    ALTER TABLE [Destinataire] DROP COLUMN [ExternalRecipientId];
                END
            ");
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
