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

            // Seed AlertType
            if (!await context.AlertType.AnyAsync())
            {
                context.AlertType.AddRange(
                    new AlertType { AlertTypeName = "acquittementNécessaire" },
                    new AlertType { AlertTypeName = "acquittementNonNécessaire" }
                );
            }

            // Seed ExpedType
            if (!await context.ExpedType.AnyAsync())
            {
                context.ExpedType.AddRange(
                    new ExpedType { ExpedTypeName = "Humain" },
                    new ExpedType { ExpedTypeName = "Service" }
                );
            }

            // Seed Statut
            if (!await context.Statut.AnyAsync())
            {
                context.Statut.AddRange(
                    new Statut { StatutName = "En Cours" },
                    new Statut { StatutName = "Terminé" },
                    new Statut { StatutName = "Échoué" }
                );
            }

            // Seed Etat
            if (!await context.Etat.AnyAsync())
            {
                context.Etat.AddRange(
                    new Etat { EtatAlerteName = "Non Lu" },
                    new Etat { EtatAlerteName = "Lu" }
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
        }

    }
}
