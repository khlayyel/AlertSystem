namespace AlertSystem.Service.Services
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
            { "acquittementNécessaire", TimeSpan.FromMinutes(30) }, // Obligatoire = rappels toutes les 30 min
            // Information n'a pas de rappels donc pas d'entrée dans le dictionnaire
        };

        public TimeSpan GetIntervalForAlertType(string alertType)
        {
            return AlertTypeIntervals.TryGetValue(alertType, out var interval) 
                ? interval 
                : FirstReminderDelay;
        }
    }
}
