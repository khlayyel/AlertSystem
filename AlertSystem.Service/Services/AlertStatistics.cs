namespace AlertSystem.Service.Services
{
    /// <summary>
    /// Représente les statistiques d'alertes
    /// </summary>
    public class AlertStatistics
    {
        public int TotalAlerts { get; set; }
        public int TodayAlerts { get; set; }
        public int UnreadAlerts { get; set; }
        public int SentAlerts { get; set; }
        public int FailedAlerts { get; set; }
        public int ConfirmedMandatory { get; set; }
        public int PendingMandatory { get; set; }
        public int EmailAlerts { get; set; }
        public int WhatsAppAlerts { get; set; }
        public int DesktopAlerts { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
