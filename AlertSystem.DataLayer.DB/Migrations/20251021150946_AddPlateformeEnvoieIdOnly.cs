using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPlateformeEnvoieIdOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlateformeEnvoieId",
                table: "HistoriqueAlerte",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueAlerte_PlateformeEnvoieId",
                table: "HistoriqueAlerte",
                column: "PlateformeEnvoieId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriqueAlerte_PlateformeEnvoie_PlateformeEnvoieId",
                table: "HistoriqueAlerte",
                column: "PlateformeEnvoieId",
                principalTable: "PlateformeEnvoie",
                principalColumn: "PlateformeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoriqueAlerte_PlateformeEnvoie_PlateformeEnvoieId",
                table: "HistoriqueAlerte");

            migrationBuilder.DropIndex(
                name: "IX_HistoriqueAlerte_PlateformeEnvoieId",
                table: "HistoriqueAlerte");

            migrationBuilder.DropColumn(
                name: "PlateformeEnvoieId",
                table: "HistoriqueAlerte");
        }
    }
}
