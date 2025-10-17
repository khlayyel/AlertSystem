using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlertSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreTestUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ajouter Zied Soltani et 3 autres utilisateurs de test
            migrationBuilder.Sql(@"
                INSERT INTO Users (Username, FullName, Email, PhoneNumber, WhatsAppNumber, DesktopDeviceToken, PasswordHash, Role, DepartmentId, CreatedAt, IsActive)
                VALUES 
                -- Zied Soltani
                ('zied', 'Zied Soltani', 'zied.soltani111@gmail.com', '21494064', '+21621494064', 'web-push-token-zied-001', 'temp-password-hash', 'SuperUser', NULL, GETDATE(), 1),
                
                -- Utilisateur test 1 - Sarah Ben Ali
                ('sarah', 'Sarah Ben Ali', 'sarah.benali@test.com', '20123456', '+21620123456', 'web-push-token-sarah-001', 'temp-password-hash', 'User', NULL, GETDATE(), 1),
                
                -- Utilisateur test 2 - Ahmed Trabelsi  
                ('ahmed', 'Ahmed Trabelsi', 'ahmed.trabelsi@test.com', '25987654', '+21625987654', 'web-push-token-ahmed-001', 'temp-password-hash', 'User', NULL, GETDATE(), 1),
                
                -- Utilisateur test 3 - Fatma Karray
                ('fatma', 'Fatma Karray', 'fatma.karray@test.com', '22555777', '+21622555777', 'web-push-token-fatma-001', 'temp-password-hash', 'SuperUser', NULL, GETDATE(), 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
