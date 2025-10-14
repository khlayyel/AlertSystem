using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPlateformeEnvoieAndDestinataireColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinataireId",
                table: "Alerte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlateformeEnvoieId",
                table: "Alerte",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlateformeEnvoie",
                columns: table => new
                {
                    PlateformeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Plateforme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlateformeEnvoie", x => x.PlateformeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_DestinataireId",
                table: "Alerte",
                column: "DestinataireId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_PlateformeEnvoieId",
                table: "Alerte",
                column: "PlateformeEnvoieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerte_PlateformeEnvoie_PlateformeEnvoieId",
                table: "Alerte",
                column: "PlateformeEnvoieId",
                principalTable: "PlateformeEnvoie",
                principalColumn: "PlateformeId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Alerte_Users_DestinataireId",
                table: "Alerte",
                column: "DestinataireId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerte_PlateformeEnvoie_PlateformeEnvoieId",
                table: "Alerte");

            migrationBuilder.DropForeignKey(
                name: "FK_Alerte_Users_DestinataireId",
                table: "Alerte");

            migrationBuilder.DropTable(
                name: "PlateformeEnvoie");

            migrationBuilder.DropIndex(
                name: "IX_Alerte_DestinataireId",
                table: "Alerte");

            migrationBuilder.DropIndex(
                name: "IX_Alerte_PlateformeEnvoieId",
                table: "Alerte");

            migrationBuilder.DropColumn(
                name: "DestinataireId",
                table: "Alerte");

            migrationBuilder.DropColumn(
                name: "PlateformeEnvoieId",
                table: "Alerte");
        }
    }
}
