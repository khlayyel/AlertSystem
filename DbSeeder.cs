using AlertSystem.Data;
using AlertSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

            await context.SaveChangesAsync();
        }
    }
}
