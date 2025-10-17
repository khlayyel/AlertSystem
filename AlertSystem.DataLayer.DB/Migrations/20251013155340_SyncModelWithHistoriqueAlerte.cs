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
            migrationBuilder.DropTable(
                name: "Destinataire");

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

            migrationBuilder.DropColumn(
                name: "DateLecture",
                table: "Alerte");

            migrationBuilder.DropColumn(
                name: "RappelSuivant",
                table: "Alerte");

            migrationBuilder.DropColumn(
                name: "destinataireMail",
                table: "Alerte");

            migrationBuilder.DropColumn(
                name: "destinatairedesktop",
                table: "Alerte");

            migrationBuilder.DropColumn(
                name: "destinatairenum",
                table: "Alerte");

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
