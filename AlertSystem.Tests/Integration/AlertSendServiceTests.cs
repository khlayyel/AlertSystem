using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AlertSystem.Data;
using AlertSystem.Service;
using AlertSystem.Services;
using AlertSystem.Entities.Entities;
using Xunit;

namespace AlertSystem.Tests.Integration;

public class AlertSendServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AlertSendService _service;

    public AlertSendServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"SendServiceTest_{Guid.NewGuid()}"));
        
        services.AddScoped<INotificationService, StubNotificationService>();
        services.AddScoped<AlertSystem.Services.IWhatsAppService, StubWhatsAppService>();
        services.AddScoped<ConfirmationTokenService>(provider =>
            new ConfirmationTokenService("test-secret"));
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddHttpContextAccessor();
        services.AddLogging();
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BASE_URL"] = "http://localhost:5185"
            })
            .Build();
        services.AddSingleton<IConfiguration>(config);
        
        var provider = services.BuildServiceProvider();
        _db = provider.GetRequiredService<ApplicationDbContext>();
        
        _service = new AlertSendService(
            _db,
            provider.GetRequiredService<INotificationService>(),
            provider.GetRequiredService<AlertSystem.Services.IWhatsAppService>(),
            config,
            provider.GetRequiredService<ConfirmationTokenService>(),
            provider.GetRequiredService<IEmailTemplateService>(),
            provider.GetRequiredService<IHttpContextAccessor>(),
            provider.GetRequiredService<ILogger<AlertSendService>>()
        );
        
        SeedReferenceData();
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
    public async Task SendManualAsync_AllChannelsSuccess_ReturnsSuccessResponse()
    {
        // Act
        var response = await _service.SendManualAsync(
            "Test Alert",
            "Test Message",
            emails: new[] { "test@example.com" },
            phones: new[] { "+1234567890" },
            sendEmail: true,
            sendWhatsApp: true,
            sendDesktop: false,
            userIds: null,
            alertTypeId: 1
        );

        // Assert
        Assert.True(response.OverallSuccess);
        Assert.True(response.AlerteId > 0);
        Assert.Equal(2, response.Results.Count);
        
        var emailResult = response.Results.First(r => r.Type == "email");
        Assert.True(emailResult.Success);
        Assert.Equal("test@example.com", emailResult.Recipient);
        
        var whatsappResult = response.Results.First(r => r.Type == "whatsapp");
        Assert.True(whatsappResult.Success);
        Assert.Equal("+1234567890", whatsappResult.Recipient);
        
        // Verify database state
        var alert = await _db.Alerte.FindAsync(response.AlerteId);
        Assert.NotNull(alert);
        Assert.Equal(2, alert.StatutId); // Envoyé
        // Originating user fields removed; ensure core fields are set
        
        var historique = await _db.HistoriqueAlertes
            .Where(h => h.AlerteId == response.AlerteId)
            .ToListAsync();
        Assert.Equal(2, historique.Count);
    }

    [Fact]
    public async Task SendManualAsync_PartialFailure_ReturnsPartialSuccessResponse()
    {
        // Arrange
        var failingService = new AlertSendService(
            _db,
            new FailingNotificationService(), // Will fail all sends
            new FailingWhatsAppService(), // Will fail all WhatsApp sends
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BASE_URL"] = "http://localhost:5185"
            }).Build(),
            new ConfirmationTokenService("test-secret"),
            new EmailTemplateService(),
            new HttpContextAccessor(), // Added
            new LoggerFactory().CreateLogger<AlertSendService>() // Added
        );

        // Act
        var response = await failingService.SendManualAsync(
            "Test Alert",
            "Test Message",
            emails: new[] { "test@example.com" },
            phones: new[] { "+1234567890" },
            sendEmail: true,
            sendWhatsApp: true,
            sendDesktop: false,
            userIds: null,
            alertTypeId: 1
        );

        // Assert
        Assert.False(response.OverallSuccess);
        Assert.True(response.AlerteId > 0);
        Assert.Equal(2, response.Results.Count);
        
        foreach (var result in response.Results)
        {
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }
        
        // Verify database state
        var alert = await _db.Alerte.FindAsync(response.AlerteId);
        Assert.NotNull(alert);
        Assert.Equal(4, alert.StatutId); // Échoué
    }

    [Fact]
    public async Task SendManualAsync_EmptyRecipients_ReturnsSuccessWithNoResults()
    {
        // Act
        var response = await _service.SendManualAsync(
            "Test Alert",
            "Test Message",
            emails: Array.Empty<string>(),
            phones: Array.Empty<string>(),
            sendEmail: false,
            sendWhatsApp: false,
            sendDesktop: false,
            userIds: null,
            alertTypeId: 1
        );

        // Assert
        Assert.True(response.OverallSuccess);
        Assert.True(response.AlerteId > 0);
        Assert.Empty(response.Results);
        
        // Verify database state
        var alert = await _db.Alerte.FindAsync(response.AlerteId);
        Assert.NotNull(alert);
        Assert.Equal(2, alert.StatutId); // Envoyé (no sends attempted = success)
    }

    [Fact]
    public async Task SendManualAsync_DesktopWithValidUserIds_CreatesHistorique()
    {
        // Arrange
        _db.Users.Add(new User
        {
            UserId = 1,
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            DesktopDeviceToken = "desktop-token-123"
        });
        await _db.SaveChangesAsync();

        // Act
        var response = await _service.SendManualAsync(
            "Test Alert",
            "Test Message",
            emails: Array.Empty<string>(),
            phones: Array.Empty<string>(),
            sendEmail: false,
            sendWhatsApp: false,
            sendDesktop: true,
            userIds: new[] { 1 },
            alertTypeId: 1
        );

        // Assert
        Assert.True(response.OverallSuccess);
        Assert.Single(response.Results);
        
        var desktopResult = response.Results.First();
        Assert.Equal("desktop", desktopResult.Type);
        Assert.Equal("1", desktopResult.Recipient);
        Assert.True(desktopResult.Success);
        
        // Verify database state
        var historique = await _db.HistoriqueAlertes
            .Where(h => h.AlerteId == response.AlerteId)
            .ToListAsync();
        Assert.Single(historique);
        Assert.Equal(1, historique[0].DestinataireUserId);
    }

    [Fact]
    public async Task SendManualAsync_DesktopWithInvalidUserIds_SkipsInvalidUsers()
    {
        // Act
        var response = await _service.SendManualAsync(
            "Test Alert",
            "Test Message",
            emails: Array.Empty<string>(),
            phones: Array.Empty<string>(),
            sendEmail: false,
            sendWhatsApp: false,
            sendDesktop: true,
            userIds: new[] { 999 }, // Non-existent user
            alertTypeId: 1
        );

        // Assert
        Assert.True(response.OverallSuccess);
        Assert.Empty(response.Results); // No valid users to send to
        
        // Verify database state
        var historique = await _db.HistoriqueAlertes
            .Where(h => h.AlerteId == response.AlerteId)
            .ToListAsync();
        Assert.Empty(historique); // No history created for invalid users
    }

    // Test removed: OriginatingUser fields no longer stored

    public void Dispose()
    {
        _db?.Dispose();
    }
}

// Test services
public class StubNotificationService : INotificationService
{
    public Task<bool> SendEmailAsync(string toEmail, string subject, string message) => 
        Task.FromResult(true);

    public Task<bool> SendHtmlEmailAsync(string toEmail, string subject, string htmlContent) => 
        Task.FromResult(true);

    public Task<bool> SendWhatsAppAsync(string phoneNumber, string message) => 
        Task.FromResult(true);

    public Task<bool> SendPushNotificationAsync(int userId, string title, string message, string? url = null) => 
        Task.FromResult(true);

    public Task<List<string>> SendToAllPlatformsAsync(int userId, string title, string message, string? url = null) => 
        Task.FromResult(new List<string> { "email", "whatsapp", "desktop" });
}

public class FailingNotificationService : INotificationService
{
    public Task<bool> SendEmailAsync(string toEmail, string subject, string message) => 
        Task.FromResult(false);

    public Task<bool> SendHtmlEmailAsync(string toEmail, string subject, string htmlContent) => 
        Task.FromResult(false);

    public Task<bool> SendWhatsAppAsync(string phoneNumber, string message) => 
        Task.FromResult(false);

    public Task<bool> SendPushNotificationAsync(int userId, string title, string message, string? url = null) => 
        Task.FromResult(false);

    public Task<List<string>> SendToAllPlatformsAsync(int userId, string title, string message, string? url = null) => 
        Task.FromResult(new List<string>());
}

public class StubWhatsAppService : AlertSystem.Services.IWhatsAppService
{
    public Task<bool> SendMessageAsync(string phoneNumber, string message) => 
        Task.FromResult(true);

    public Task<bool> SendAlertAsync(string phoneNumber, string title, string message, string senderName) => 
        Task.FromResult(true);

    public Task<bool> SendTemplateHelloAsync(string phoneNumber) => 
        Task.FromResult(true);

    public Task<bool> SendTemplateAsync(string phoneNumber, string templateName, string languageCode, IDictionary<string, string>? variables = null) => 
        Task.FromResult(true);
}

public class FailingWhatsAppService : AlertSystem.Services.IWhatsAppService
{
    public Task<bool> SendMessageAsync(string phoneNumber, string message) => 
        Task.FromResult(false);

    public Task<bool> SendAlertAsync(string phoneNumber, string title, string message, string senderName) => 
        Task.FromResult(false);

    public Task<bool> SendTemplateHelloAsync(string phoneNumber) => 
        Task.FromResult(false);

    public Task<bool> SendTemplateAsync(string phoneNumber, string templateName, string languageCode, IDictionary<string, string>? variables = null) => 
        Task.FromResult(false);
}
