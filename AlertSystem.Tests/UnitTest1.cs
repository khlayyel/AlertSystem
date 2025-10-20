using System.Threading.Tasks;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Tests;

public class UnitTest1
{
    [Fact]
    public async Task SeedAndCreateAlert_WritesHistorique_WithEtatNonLu()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "alerts-test")
            .Options;

        using var db = new ApplicationDbContext(options);
        await AlertSystem.DbSeeder.SeedAsync(db);

        var statutId = await db.Statut.Where(s => s.StatutName == "En Cours").Select(s => s.StatutId).FirstAsync();
        var etatId = await db.Etat.Where(e => e.EtatAlerteName == "Non Lu").Select(e => e.EtatAlerteId).FirstAsync();

        var a = new Alerte
        {
            TitreAlerte = "Test",
            DescriptionAlerte = "Message",
            AlertTypeId = 1,
            StatutId = statutId,
            EtatAlerteId = etatId,
            DateCreationAlerte = System.DateTime.UtcNow
        };
        db.Alerte.Add(a);
        await db.SaveChangesAsync();

        var h = new HistoriqueAlerte { AlerteId = a.AlerteId, DestinataireEmail = "t@t.com", EtatAlerteId = etatId };
        db.HistoriqueAlertes.Add(h);
        await db.SaveChangesAsync();

        var saved = await db.HistoriqueAlertes.FirstAsync();
        Assert.Equal(etatId, saved.EtatAlerteId);
    }
}
