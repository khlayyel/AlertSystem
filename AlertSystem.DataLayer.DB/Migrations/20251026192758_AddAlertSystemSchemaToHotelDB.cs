using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.DataLayer.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertSystemSchemaToHotelDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertProcessingQueue",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertRecordId = table.Column<int>(type: "int", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertProcessingQueue", x => x.QueueId);
                });

            migrationBuilder.CreateTable(
                name: "AlertType",
                columns: table => new
                {
                    AlertTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresAcknowledgment = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertType", x => x.AlertTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ApiClients",
                columns: table => new
                {
                    ApiClientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClients", x => x.ApiClientId);
                });

            migrationBuilder.CreateTable(
                name: "def_utilisateur",
                columns: table => new
                {
                    util_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    util_nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    util_email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    util_telephone_portable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    util_actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_def_utilisateur", x => x.util_id);
                });

            migrationBuilder.CreateTable(
                name: "Etat",
                columns: table => new
                {
                    EtatAlerteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EtatAlerte = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etat", x => x.EtatAlerteId);
                });

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

            migrationBuilder.CreateTable(
                name: "Statut",
                columns: table => new
                {
                    StatutId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statut", x => x.StatutId);
                });

            migrationBuilder.CreateTable(
                name: "WebPushSubscriptions",
                columns: table => new
                {
                    WebPushSubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebPushSubscriptions", x => x.WebPushSubscriptionId);
                });

            migrationBuilder.CreateTable(
                name: "Alerte",
                columns: table => new
                {
                    AlertRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertTypeId = table.Column<int>(type: "int", nullable: false),
                    AppId = table.Column<int>(type: "int", nullable: true),
                    ExpediteurId = table.Column<int>(type: "int", nullable: true),
                    TitreAlerte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptionAlerte = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreationAlerte = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatutId = table.Column<int>(type: "int", nullable: false),
                    EtatAlerteId = table.Column<int>(type: "int", nullable: false),
                    PlateformeEnvoieId = table.Column<int>(type: "int", nullable: false),
                    DestinataireUserId = table.Column<int>(type: "int", nullable: true),
                    DestinataireEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinatairePhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinataireDesktop = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateLecture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RappelSuivant = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedByWorker = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PlateformeEnvoiePlateformeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerte", x => x.AlertRecordId);
                    table.ForeignKey(
                        name: "FK_Alerte_AlertType_AlertTypeId",
                        column: x => x.AlertTypeId,
                        principalTable: "AlertType",
                        principalColumn: "AlertTypeId");
                    table.ForeignKey(
                        name: "FK_Alerte_Etat_EtatAlerteId",
                        column: x => x.EtatAlerteId,
                        principalTable: "Etat",
                        principalColumn: "EtatAlerteId");
                    table.ForeignKey(
                        name: "FK_Alerte_PlateformeEnvoie_PlateformeEnvoieId",
                        column: x => x.PlateformeEnvoieId,
                        principalTable: "PlateformeEnvoie",
                        principalColumn: "PlateformeId");
                    table.ForeignKey(
                        name: "FK_Alerte_PlateformeEnvoie_PlateformeEnvoiePlateformeId",
                        column: x => x.PlateformeEnvoiePlateformeId,
                        principalTable: "PlateformeEnvoie",
                        principalColumn: "PlateformeId");
                    table.ForeignKey(
                        name: "FK_Alerte_Statut_StatutId",
                        column: x => x.StatutId,
                        principalTable: "Statut",
                        principalColumn: "StatutId");
                    table.ForeignKey(
                        name: "FK_Alerte_def_utilisateur_DestinataireUserId",
                        column: x => x.DestinataireUserId,
                        principalTable: "def_utilisateur",
                        principalColumn: "util_id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Alerte_def_utilisateur_ExpediteurId",
                        column: x => x.ExpediteurId,
                        principalTable: "def_utilisateur",
                        principalColumn: "util_id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "RappelSuivant",
                columns: table => new
                {
                    RappelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlerteId = table.Column<int>(type: "int", nullable: false),
                    AlertRecordId = table.Column<int>(type: "int", nullable: false),
                    DateRappel = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    StatutRappel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tentative = table.Column<int>(type: "int", nullable: false),
                    DetailsErreur = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RappelSuivant", x => x.RappelId);
                    table.ForeignKey(
                        name: "FK_RappelSuivant_Alerte_AlerteId",
                        column: x => x.AlerteId,
                        principalTable: "Alerte",
                        principalColumn: "AlertRecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_AlertGroupId",
                table: "Alerte",
                column: "AlertGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_AlertTypeId",
                table: "Alerte",
                column: "AlertTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_DestinataireUserId",
                table: "Alerte",
                column: "DestinataireUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_EtatAlerteId",
                table: "Alerte",
                column: "EtatAlerteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_ExpediteurId",
                table: "Alerte",
                column: "ExpediteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_PlateformeEnvoieId",
                table: "Alerte",
                column: "PlateformeEnvoieId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_PlateformeEnvoiePlateformeId",
                table: "Alerte",
                column: "PlateformeEnvoiePlateformeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerte_StatutId_ProcessedByWorker_DateCreationAlerte",
                table: "Alerte",
                columns: new[] { "StatutId", "ProcessedByWorker", "DateCreationAlerte" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertProcessingQueue_AlertRecordId",
                table: "AlertProcessingQueue",
                column: "AlertRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertProcessingQueue_Priority_QueuedAt",
                table: "AlertProcessingQueue",
                columns: new[] { "Priority", "QueuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RappelSuivant_AlerteId",
                table: "RappelSuivant",
                column: "AlerteId");

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_UserId_Endpoint",
                table: "WebPushSubscriptions",
                columns: new[] { "UserId", "Endpoint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertProcessingQueue");

            migrationBuilder.DropTable(
                name: "ApiClients");

            migrationBuilder.DropTable(
                name: "RappelSuivant");

            migrationBuilder.DropTable(
                name: "WebPushSubscriptions");

            migrationBuilder.DropTable(
                name: "Alerte");

            migrationBuilder.DropTable(
                name: "AlertType");

            migrationBuilder.DropTable(
                name: "Etat");

            migrationBuilder.DropTable(
                name: "PlateformeEnvoie");

            migrationBuilder.DropTable(
                name: "Statut");

            migrationBuilder.DropTable(
                name: "def_utilisateur");
        }
    }
}
