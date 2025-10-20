using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithHistoriqueAlerte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop legacy table only if it exists (guard for fresh DB)
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Destinataire]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [Destinataire];
                END
            ");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Users");

            // Guarded removal of legacy columns from Alerte for fresh DBs
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Alerte]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Alerte','DateLecture') IS NOT NULL
                    BEGIN
                        DECLARE @c1 sysname;
                        SELECT @c1 = [d].[name]
                        FROM [sys].[default_constraints] [d]
                        INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                        WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Alerte]') AND [c].[name] = N'DateLecture');
                        IF @c1 IS NOT NULL EXEC(N'ALTER TABLE [Alerte] DROP CONSTRAINT [' + @c1 + ']');
                        ALTER TABLE [Alerte] DROP COLUMN [DateLecture];
                    END

                    IF COL_LENGTH('Alerte','RappelSuivant') IS NOT NULL
                        ALTER TABLE [Alerte] DROP COLUMN [RappelSuivant];

                    IF COL_LENGTH('Alerte','destinataireMail') IS NOT NULL
                        ALTER TABLE [Alerte] DROP COLUMN [destinataireMail];

                    IF COL_LENGTH('Alerte','destinatairedesktop') IS NOT NULL
                        ALTER TABLE [Alerte] DROP COLUMN [destinatairedesktop];

                    IF COL_LENGTH('Alerte','destinatairenum') IS NOT NULL
                        ALTER TABLE [Alerte] DROP COLUMN [destinatairenum];
                END
            ");

            // Ensure Alerte table exists for FK (minimal schema if missing)
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Alerte]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Alerte] (
                        [AlerteId] INT NOT NULL IDENTITY(1,1),
                        CONSTRAINT [PK_Alerte] PRIMARY KEY ([AlerteId])
                    );
                END
            ");

            migrationBuilder.CreateTable(
                name: "HistoriqueAlerte",
                columns: table => new
                {
                    DestinataireId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlerteId = table.Column<int>(type: "int", nullable: false),
                    DestinataireUserId = table.Column<int>(type: "int", nullable: false),
                    EtatAlerte = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateLecture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RappelSuivant = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DestinataireEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinatairePhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinataireDesktop = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueAlerte", x => x.DestinataireId);
                    table.ForeignKey(
                        name: "FK_HistoriqueAlerte_Alerte_AlerteId",
                        column: x => x.AlerteId,
                        principalTable: "Alerte",
                        principalColumn: "AlerteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoriqueAlerte_Users_DestinataireUserId",
                        column: x => x.DestinataireUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueAlerte_AlerteId",
                table: "HistoriqueAlerte",
                column: "AlerteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueAlerte_DestinataireUserId",
                table: "HistoriqueAlerte",
                column: "DestinataireUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriqueAlerte");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLecture",
                table: "Alerte",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RappelSuivant",
                table: "Alerte",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destinataireMail",
                table: "Alerte",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destinatairedesktop",
                table: "Alerte",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "destinatairenum",
                table: "Alerte",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Destinataire",
                columns: table => new
                {
                    DestinataireId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlerteId = table.Column<int>(type: "int", nullable: false),
                    DateLecture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EtatAlerte = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinataire", x => x.DestinataireId);
                    table.ForeignKey(
                        name: "FK_Destinataire_Alerte_AlerteId",
                        column: x => x.AlerteId,
                        principalTable: "Alerte",
                        principalColumn: "AlerteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Destinataire_AlerteId",
                table: "Destinataire",
                column: "AlerteId");
        }
    }
}
