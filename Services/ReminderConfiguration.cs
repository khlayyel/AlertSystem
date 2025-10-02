namespace AlertSystem.Services
{
    public sealed class ReminderConfiguration
    {
        public TimeSpan FirstReminderDelay { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan SubsequentReminderInterval { get; set; } = TimeSpan.FromHours(1);
        public int MaxReminders { get; set; } = 5;
        public TimeSpan ServiceCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableReminders { get; set; } = true;
        
        // Intervalles par type d'alerte
        public Dictionary<string, TimeSpan> AlertTypeIntervals { get; set; } = new()
        {
            { "Obligatoire", TimeSpan.FromMinutes(30) },
            { "Information", TimeSpan.FromHours(24) } // Pas de reminders pour les infos par défaut
        };

        public TimeSpan GetIntervalForAlertType(string alertType)
        {
            return AlertTypeIntervals.TryGetValue(alertType, out var interval) 
                ? interval 
                : FirstReminderDelay;
        }
    }
}
