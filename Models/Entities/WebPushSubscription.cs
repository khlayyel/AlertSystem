namespace AlertSystem.Models.Entities
{
    public sealed class WebPushSubscription
    {
        public int WebPushSubscriptionId { get; set; }
        public int UserId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


