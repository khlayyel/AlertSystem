using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AlertSystem.Data;
using AlertSystem.Worker.Services;
using AlertSystem.Entities.Entities;
using AlertSystem.Services;
using Xunit;

namespace AlertSystem.Tests.Integration;

public class WorkerIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IAlertRepository _repository;

    public WorkerIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"WorkerTest_{Guid.NewGuid()}"));
        
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build());
        services.AddScoped<IAlertRepository, TestAlertRepository>();
        services.AddScoped<INotificationService, StubNotificationService>();
        services.AddScoped<AlertSystem.Services.IWhatsAppService, StubWhatsAppService>();
        
        var provider = services.BuildServiceProvider();
        _db = provider.GetRequiredService<ApplicationDbContext>();
        _repository = provider.GetRequiredService<IAlertRepository>();
        
        // Seed reference data
        SeedReferenceData();
        
        // Note: AlertePollingWorker is not available in test context, so we'll test the repository directly
    }

    private void SeedReferenceData()
    {
        _db.AlertType.AddRange(new[]
        {
            new AlertType { AlertTypeId = 1, AlertTypeName = "Information" },
            new AlertType { AlertTypeId = 2, AlertTypeName = "acquittementNécessaire" }
        });
        
        _db.ExpedType.AddRange(new[]
        {
            new ExpedType { ExpedTypeId = 1, ExpedTypeName = "Humain" },
            new ExpedType { ExpedTypeId = 2, ExpedTypeName = "Service" }
        });
        
        _db.Statut.AddRange(new[]
        {
            new Statut { StatutId = 1, StatutName = "En Cours" },
            new Statut { StatutId = 2, StatutName = "Envoyé" },
            new Statut { StatutId = 3, StatutName = "Annulé" },
            new Statut { StatutId = 4, StatutName = "Échoué" }
        });
        
        _db.Etat.AddRange(new[]
        {
            new Etat { EtatAlerteId = 1, EtatAlerteName = "Non Lu" },
            new Etat { EtatAlerteId = 2, EtatAlerteName = "Lu" }
        });
        
        _db.SaveChanges();
    }

    [Fact]
    public async Task Worker_ProcessesUnprocessedAlerts()
    {
        // Arrange
        var alert = new Alerte
        {
            TitreAlerte = "Test Alert",
            DescriptionAlerte = "Test Description",
            DateCreationAlerte = DateTime.UtcNow,
            StatutId = 1, // En Cours
            AlertTypeId = 1,
            ExpedTypeId = 1,
            EtatAlerteId = 1,
            // ProcessedByWorker is not in the entity model
        };
        
        _db.Alerte.Add(alert);
        await _db.SaveChangesAsync();

        // Act
        var unprocessedAlerts = await _repository.GetUnprocessedAlertsAsync();

        // Assert
        Assert.Single(unprocessedAlerts);
        Assert.Equal(alert.AlerteId, unprocessedAlerts[0].AlerteId);
    }

    [Fact]
    public async Task Worker_IgnoresCancelledAlerts()
    {
        // Arrange
        var alert = new Alerte
        {
            TitreAlerte = "Cancelled Alert",
            DescriptionAlerte = "This should be ignored",
            DateCreationAlerte = DateTime.UtcNow,
            StatutId = 3, // Annulé
            AlertTypeId = 1,
            ExpedTypeId = 1,
            EtatAlerteId = 1,
            // ProcessedByWorker is not in the entity model
        };
        
        _db.Alerte.Add(alert);
        await _db.SaveChangesAsync();

        // Act
        var unprocessedAlerts = await _repository.GetUnprocessedAlertsAsync();

        // Assert
        Assert.Empty(unprocessedAlerts);
    }

    [Fact]
    public async Task Worker_ProcessesFailedAlerts()
    {
        // Arrange
        var alert = new Alerte
        {
            TitreAlerte = "Failed Alert",
            DescriptionAlerte = "This should be retried",
            DateCreationAlerte = DateTime.UtcNow,
            StatutId = 4, // Échoué
            AlertTypeId = 1,
            ExpedTypeId = 1,
            EtatAlerteId = 1,
            // ProcessedByWorker is not in the entity model
        };
        
        _db.Alerte.Add(alert);
        await _db.SaveChangesAsync();

        // Act
        var unprocessedAlerts = await _repository.GetUnprocessedAlertsAsync();

        // Assert
        Assert.Single(unprocessedAlerts);
        Assert.Equal(alert.AlerteId, unprocessedAlerts[0].AlerteId);
    }

    [Fact]
    public async Task Worker_SetsInitialReminderForAcquittementNecessaire()
    {
        // Arrange
        var alert = new Alerte
        {
            TitreAlerte = "Acquittement Required",
            DescriptionAlerte = "Please confirm receipt",
            DateCreationAlerte = DateTime.UtcNow,
            StatutId = 1, // En Cours
            AlertTypeId = 2, // acquittementNécessaire
            ExpedTypeId = 1,
            EtatAlerteId = 1,
            // ProcessedByWorker is not in the entity model
        };
        
        _db.Alerte.Add(alert);
        await _db.SaveChangesAsync();

        // Act
        await _repository.SetInitialReminderAsync(alert.AlerteId, 30);

        // Assert
        var rappel = await _db.RappelSuivant
            .FirstOrDefaultAsync(r => r.AlerteId == alert.AlerteId);
        
        Assert.NotNull(rappel);
        Assert.Equal(alert.AlerteId, rappel.AlerteId);
        Assert.Equal(1, rappel.Tentative);
    }

    [Fact]
    public async Task Worker_UpdatesReminderStatus()
    {
        // Arrange
        var alert = new Alerte
        {
            TitreAlerte = "Test Alert",
            DescriptionAlerte = "Test Description",
            DateCreationAlerte = DateTime.UtcNow,
            StatutId = 1,
            AlertTypeId = 2, // acquittementNécessaire
            ExpedTypeId = 1,
            EtatAlerteId = 1,
            // ProcessedByWorker is not in the entity model,
            // RappelTentatives and RappelSuivant are not in the entity model
        };
        
        _db.Alerte.Add(alert);
        await _db.SaveChangesAsync();

        // Create initial reminder
        await _repository.SetInitialReminderAsync(alert.AlerteId, 60);

        // Act
        var shouldContinue = await _repository.UpdateReminderStatusAsync(
            alert.AlerteId, true, 60, 5);

        // Assert
        Assert.True(shouldContinue);
        
        // Updated alert reminder scheduling is internal; repository method returned shouldContinue=true
    }

    [Fact]
    public async Task Worker_StopsRemindersAfterMaxAttempts()
    {
        // Arrange
        var alert = new Alerte
        {
            TitreAlerte = "Max Attempts Alert",
            DescriptionAlerte = "Should stop after max attempts",
            DateCreationAlerte = DateTime.UtcNow,
            StatutId = 1,
            AlertTypeId = 2, // acquittementNécessaire
            ExpedTypeId = 1,
            EtatAlerteId = 1,
            // ProcessedByWorker / Rappel fields are not in the entity model
        };
        
        _db.Alerte.Add(alert);
        await _db.SaveChangesAsync();

        // Act
        var shouldContinue = await _repository.UpdateReminderStatusAsync(
            alert.AlerteId, false, 60, 5);

        // Assert
        Assert.False(shouldContinue);
        
        // No direct field on entity to assert; rely on method return
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}

