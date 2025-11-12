using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using AlertSystem.Utils.Crypto;

namespace AlertSystem
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Ensure auxiliary tables that are not in SQL script exist (e.g., ApiClients)
            // This protects when the DB was created from scripts that didn't include these tables.
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.ApiClients', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiClients (
        ApiClientId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] nvarchar(max) NOT NULL,
        ApiKeyHash nvarchar(max) NOT NULL,
        IsActive bit NOT NULL,
        CreatedAt datetime2 NOT NULL,
        RateLimitPerMinute int NULL
    );
END
");

            // Ensure ExpediteurId column exists on Alerte (legacy dashboards filter by sender)
            await context.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('dbo.Alerte', 'ExpediteurId') IS NULL
BEGIN
    ALTER TABLE dbo.Alerte ADD ExpediteurId decimal(18,2) NULL;
END
");

            // Ensure def_App has required rows referenced by users
            if (!await context.DefApp.AnyAsync())
            {
                // Create a default App row if table is empty
                context.DefApp.Add(new DefApp { AppId = 1, Description = "Default App" });
            }
            var userAppIds = await context.DefUtilisateur.Select(u => u.AppId).Distinct().ToListAsync();
            var existingAppIds = await context.DefApp.Select(a => a.AppId).ToListAsync();
            foreach (var missing in userAppIds.Except(existingAppIds))
            {
                context.DefApp.Add(new DefApp { AppId = missing, Description = $"Application {missing}" });
            }

            // Ensure WebPushSubscriptions table exists (used by Web Push feature)
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.WebPushSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebPushSubscriptions (
        WebPushSubscriptionId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [Endpoint] nvarchar(450) NOT NULL,
        [P256dh] nvarchar(max) NOT NULL,
        [Auth] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT DF_WebPushSubscriptions_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_WebPush_User_Endpoint ON dbo.WebPushSubscriptions(UserId, Endpoint);
END
");

            // Ensure system roles table exists and seeded, and users have RoleId
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.def_system_roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.def_system_roles(
        RoleId int NOT NULL PRIMARY KEY,
        [Description] nvarchar(100) NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.def_system_roles WHERE RoleId = 1)
    INSERT INTO dbo.def_system_roles(RoleId, [Description]) VALUES (1, N'Admin');
IF NOT EXISTS (SELECT 1 FROM dbo.def_system_roles WHERE RoleId = 2)
    INSERT INTO dbo.def_system_roles(RoleId, [Description]) VALUES (2, N'User');

IF COL_LENGTH('dbo.def_Utilisateur','RoleId') IS NULL
BEGIN
    ALTER TABLE dbo.def_Utilisateur ADD RoleId int NOT NULL CONSTRAINT DF_def_Utilisateur_RoleId DEFAULT(2);
    ALTER TABLE dbo.def_Utilisateur WITH CHECK ADD CONSTRAINT FK_def_Utilisateur_Role FOREIGN KEY(RoleId) REFERENCES dbo.def_system_roles(RoleId);
END
");

            // Seed DefTypeEnvoie
            if (!await context.DefTypeEnvoie.AnyAsync())
            {
                context.DefTypeEnvoie.AddRange(
                    new DefTypeEnvoie { TypeEnvoieId = 1, Description = "Information" },
                    new DefTypeEnvoie { TypeEnvoieId = 2, Description = "Obligatoire" }
                );
            }

            // Seed Statut (1..4)
            if (!await context.Statut.AnyAsync())
            {
                context.Statut.AddRange(
                    new Statut { StatutId = 1, Description = "EnCours" },
                    new Statut { StatutId = 2, Description = "Envoye" },
                    new Statut { StatutId = 3, Description = "Annule" },
                    new Statut { StatutId = 4, Description = "Echoue" }
                );
            }

            // Seed Etat (1..2)
            if (!await context.Etat.AnyAsync())
            {
                context.Etat.AddRange(
                    new Etat { EtatId = 1, Description = "NonLu" },
                    new Etat { EtatId = 2, Description = "Lu" }
                );
            }

            // Seed PlateformeEnvoie (1 Email, 2 WhatsApp)
            if (!await context.PlateformeEnvoie.AnyAsync())
            {
                context.PlateformeEnvoie.AddRange(
                    new PlateformeEnvoie { PlateformeId = 1, Description = "Email" },
                    new PlateformeEnvoie { PlateformeId = 2, Description = "WhatsApp" }
                );
            }

            // Seed ApiClients examples
            if (!await context.ApiClients.AnyAsync())
            {
                context.ApiClients.AddRange(
                    new ApiClient 
                    { 
                        Name = "Hotel Riviera - Production", 
                        ApiKeyHash = CryptoUtils.ComputeSha256("hotel-riviera-prod-key-2024-abc123def456"),
                        IsActive = true,
                        RateLimitPerMinute = 1000,
                        CreatedAt = DateTime.UtcNow
                    }
                );
            }

            await context.SaveChangesAsync();
        }

    }
}
