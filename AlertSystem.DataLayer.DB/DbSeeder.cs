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

            // Seed AlertType - NO ACCENTS
            if (!await context.AlertType.AnyAsync())
            {
                context.AlertType.AddRange(
                    new AlertType { AlertTypeName = "acquittementNecessaire" },
                    new AlertType { AlertTypeName = "acquittementNonNecessaire" }
                );
            }

            // Seed Statut - NO ACCENTS
            if (!await context.Statut.AnyAsync())
            {
                context.Statut.AddRange(
                    new Statut { StatutName = "EnCours" },        // ID 1
                    new Statut { StatutName = "Envoye" },         // ID 2
                    new Statut { StatutName = "Annule" },         // ID 3
                    new Statut { StatutName = "Echoue" }          // ID 4
                );
            }

            // Seed Etat - NO ACCENTS (1=NonLu, 2=Lu to match code expectations)
            if (!await context.Etat.AnyAsync())
            {
                context.Etat.AddRange(
                    new Etat { EtatAlerteName = "NonLu" },      // ID 1
                    new Etat { EtatAlerteName = "Lu" }          // ID 2
                );
            }

            // Seed PlateformeEnvoie required by Alerte FK (1=Email, 2=WhatsApp, 3=Desktop)
            if (!await context.PlateformeEnvoie.AnyAsync())
            {
                context.PlateformeEnvoie.AddRange(
                    new PlateformeEnvoie { PlateformeId = 1, Plateforme = "Email" },
                    new PlateformeEnvoie { PlateformeId = 2, Plateforme = "WhatsApp" },
                    new PlateformeEnvoie { PlateformeId = 3, Plateforme = "Desktop" }
                );
            }

            // Seed ApiClients with multiple test clients
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
                    },
                    new ApiClient 
                    { 
                        Name = "Hotel Paradise - Staging", 
                        ApiKeyHash = CryptoUtils.ComputeSha256("hotel-paradise-staging-key-2024-xyz789uvw012"),
                        IsActive = true,
                        RateLimitPerMinute = 500,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ApiClient 
                    { 
                        Name = "Hotel Oasis - Development", 
                        ApiKeyHash = CryptoUtils.ComputeSha256("hotel-oasis-dev-key-2024-mno345pqr678"),
                        IsActive = true,
                        RateLimitPerMinute = 200,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ApiClient 
                    { 
                        Name = "Hotel Sunset - Testing", 
                        ApiKeyHash = CryptoUtils.ComputeSha256("hotel-sunset-test-key-2024-stu901vwx234"),
                        IsActive = false, // Inactive for testing
                        RateLimitPerMinute = 100,
                        CreatedAt = DateTime.UtcNow
                    }
                );
            }

            await context.SaveChangesAsync();

            // Normalize lookup values to remove accents if they slipped in
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"UPDATE dbo.Statut SET StatutName = 'EnCours' WHERE StatutId = 1;
UPDATE dbo.Statut SET StatutName = 'Envoye' WHERE StatutId = 2;
UPDATE dbo.Statut SET StatutName = 'Annule' WHERE StatutId = 3;
UPDATE dbo.Statut SET StatutName = 'Echoue' WHERE StatutId = 4;
UPDATE dbo.Etat SET EtatAlerteName = 'NonLu' WHERE EtatAlerteId = 1;
UPDATE dbo.Etat SET EtatAlerteName = 'Lu' WHERE EtatAlerteId = 2;
UPDATE dbo.AlertType SET AlertTypeName = 'acquittementNecessaire' WHERE AlertTypeId = 1;
UPDATE dbo.AlertType SET AlertTypeName = 'acquittementNonNecessaire' WHERE AlertTypeId = 2;");
            }
            catch { }
        }

    }
}
