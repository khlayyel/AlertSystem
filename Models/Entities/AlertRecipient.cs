namespace AlertSystem.Models.Entities
{
    public sealed class AlertRecipient
    {
        public int AlertRecipientId { get; set; }
        public int AlertId { get; set; }
        public int UserId { get; set; }
        public bool IsConfirmed { get; set; } = false;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public DateTime? NextReminderAt { get; set; }
        public string DeliveryPlatforms { get; set; } = "[]"; // JSON array: ["Email", "Push", "WhatsApp"]
        public string SendStatus { get; set; } = "Pending"; // Pending, Sending, Sent, Failed, Cancelled

        public Alert? Alert { get; set; }
        public User? User { get; set; }
    }
}

