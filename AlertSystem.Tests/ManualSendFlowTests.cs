using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AlertSystem.Data;
using AlertSystem.Entities.Entities;
using AlertSystem.Service;
using AlertSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AlertSystem.Tests.Integration;

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
        var service = new AlertSendService(db, notify, new StubWhatsAppService(), config, tokens, emailTemplate, new MockWhatsAppTemplateService(), new HttpContextAccessor(), new LoggerFactory().CreateLogger<AlertSendService>(), new MockKpiUpdateService());

        var response = await service.SendManualAsync(
            "Title", "Body",
            emails: new[] { "t@test.com" },
            phones: new[] { "+21611111111" },
            sendEmail: true, sendWhatsApp: true, sendDesktop: false,
            userIds: null, alertTypeId: 1);

        Assert.True(response.OverallSuccess);
        var alert = await db.Alerte.FirstAsync(a => a.AlerteId == response.AlerteId);
        Assert.Equal(2, alert.StatutId); // Envoyé
        Assert.True(await db.HistoriqueAlertes.Where(h => h.AlerteId == response.AlerteId).AnyAsync());
    }

    [Fact]
    public async Task ManualSend_AllFail_SetsStatusEchoue()
    {
        var db = await CreateDbAsync("manual-fail");
        var notify = new StubNotificationService(false);
        var config = new ConfigurationBuilder().Build();
        var tokens = new ConfirmationTokenService("test-secret");
        var emailTemplate = new EmailTemplateService();
        var service = new AlertSendService(db, notify, new StubWhatsAppService(), config, tokens, emailTemplate, new MockWhatsAppTemplateService(), new HttpContextAccessor(), new LoggerFactory().CreateLogger<AlertSendService>(), new MockKpiUpdateService());

        var response = await service.SendManualAsync(
            "Title", "Body",
            emails: new[] { "t@test.com" },
            phones: new[] { "+21611111111" },
            sendEmail: true, sendWhatsApp: true, sendDesktop: false,
            userIds: null, alertTypeId: 1);

        Assert.False(response.OverallSuccess);
        var alert = await db.Alerte.FirstAsync(a => a.AlerteId == response.AlerteId);
        Assert.Equal(4, alert.StatutId); // Échoué
    }
}

    public class MockKpiUpdateService : IKpiUpdateService
    {
        public Task SendInboxKpiUpdateAsync(int userId) => Task.CompletedTask;
        public Task SendOutboxKpiUpdateAsync(int userId) => Task.CompletedTask;
        public Task SendAllKpiUpdateAsync(int userId) => Task.CompletedTask;
        public Task SendOutboxModalUpdateAsync(int userId, int alerteId, object recipientData) => Task.CompletedTask;
        public Task SendTestOutboxKpiUpdateAsync(int userId, object testData) => Task.CompletedTask;
    }

    public class MockWhatsAppTemplateService : IWhatsAppTemplateService
    {
        public Task<bool> SendAlertTemplateAsync(string phoneNumber, string title, string message, string senderName, string confirmationUrl) => Task.FromResult(true);
    }


