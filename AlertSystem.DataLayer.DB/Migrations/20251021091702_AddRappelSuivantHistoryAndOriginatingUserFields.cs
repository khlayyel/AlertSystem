using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRappelSuivantHistoryAndOriginatingUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "StatutRappel",
                table: "RappelSuivant",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateRappel",
                table: "RappelSuivant",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "DetailsErreur",
                table: "RappelSuivant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HistoriqueAlerteId",
                table: "RappelSuivant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "DestinataireUserId",
                table: "HistoriqueAlerte",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "EtatAlerteId",
                table: "HistoriqueAlerte",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RappelSuivant_HistoriqueAlerteId",
                table: "RappelSuivant",
                column: "HistoriqueAlerteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueAlerte_EtatAlerteId",
                table: "HistoriqueAlerte",
                column: "EtatAlerteId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriqueAlerte_Etat_EtatAlerteId",
                table: "HistoriqueAlerte",
                column: "EtatAlerteId",
                principalTable: "Etat",
                principalColumn: "EtatAlerteId");

            migrationBuilder.AddForeignKey(
                name: "FK_RappelSuivant_HistoriqueAlerte_HistoriqueAlerteId",
                table: "RappelSuivant",
                column: "HistoriqueAlerteId",
                principalTable: "HistoriqueAlerte",
                principalColumn: "DestinataireId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoriqueAlerte_Etat_EtatAlerteId",
                table: "HistoriqueAlerte");

            migrationBuilder.DropForeignKey(
                name: "FK_RappelSuivant_HistoriqueAlerte_HistoriqueAlerteId",
                table: "RappelSuivant");

            migrationBuilder.DropIndex(
                name: "IX_RappelSuivant_HistoriqueAlerteId",
                table: "RappelSuivant");

            migrationBuilder.DropIndex(
                name: "IX_HistoriqueAlerte_EtatAlerteId",
                table: "HistoriqueAlerte");

            migrationBuilder.DropColumn(
                name: "DetailsErreur",
                table: "RappelSuivant");

            migrationBuilder.DropColumn(
                name: "HistoriqueAlerteId",
                table: "RappelSuivant");

            migrationBuilder.DropColumn(
                name: "EtatAlerteId",
                table: "HistoriqueAlerte");

            migrationBuilder.AlterColumn<string>(
                name: "StatutRappel",
                table: "RappelSuivant",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateRappel",
                table: "RappelSuivant",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "DestinataireUserId",
                table: "HistoriqueAlerte",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

        }
    }
}
