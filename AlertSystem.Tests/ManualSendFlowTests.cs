using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service;
using AlertSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Tests;

internal sealed class StubNotificationService : INotificationService
{
    private readonly bool _result;
    public StubNotificationService(bool result) { _result = result; }
    public Task<bool> SendEmailAsync(string to, string subject, string htmlBody) => Task.FromResult(_result);
    public Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlContent) => Task.FromResult(_result);
    public Task<bool> SendWhatsAppAsync(string to, string message) => Task.FromResult(_result);
    public Task<bool> SendPushNotificationAsync(int userId, string title, string message, string? url = null) => Task.FromResult(_result);
    public Task<List<string>> SendToAllPlatformsAsync(int userId, string title, string message, string? url = null) => Task.FromResult(new List<string>());
}

public class ManualSendFlowTests
{
    private static async Task<ApplicationDbContext> CreateDbAsync(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ApplicationDbContext(options);
        await AlertSystem.DbSeeder.SeedAsync(db);
        return db;
    }

    [Fact]
    public async Task ManualSend_Success_SetsStatusEnvoye_AndCreatesHistorique()
    {
        var db = await CreateDbAsync("manual-success");
        var notify = new StubNotificationService(true);
        var config = new ConfigurationBuilder().Build();
        var tokens = new ConfirmationTokenService("test-secret");
        var emailTemplate = new EmailTemplateService();
        var service = new AlertSendService(db, notify, null, config, tokens, emailTemplate);

        var (alertId, anySuccess) = await service.SendManualAsync(
            "Title", "Body",
            emails: new[] { "t@test.com" },
            phones: new[] { "+21611111111" },
            sendEmail: true, sendWhatsApp: true, sendDesktop: false,
            userIds: null, alertTypeId: 1);

        Assert.True(anySuccess);
        var alert = await db.Alerte.FirstAsync(a => a.AlerteId == alertId);
        Assert.Equal(2, alert.StatutId); // Envoyé
        Assert.True(await db.HistoriqueAlertes.Where(h => h.AlerteId == alertId).AnyAsync());
    }

    [Fact]
    public async Task ManualSend_AllFail_SetsStatusEchoue()
    {
        var db = await CreateDbAsync("manual-fail");
        var notify = new StubNotificationService(false);
        var config = new ConfigurationBuilder().Build();
        var tokens = new ConfirmationTokenService("test-secret");
        var emailTemplate = new EmailTemplateService();
        var service = new AlertSendService(db, notify, null, config, tokens, emailTemplate);

        var (alertId, anySuccess) = await service.SendManualAsync(
            "Title", "Body",
            emails: new[] { "t@test.com" },
            phones: new[] { "+21611111111" },
            sendEmail: true, sendWhatsApp: true, sendDesktop: false,
            userIds: null, alertTypeId: 1);

        Assert.False(anySuccess);
        var alert = await db.Alerte.FirstAsync(a => a.AlerteId == alertId);
        Assert.Equal(4, alert.StatutId); // Échoué
    }
}


