namespace AlertSystem.Models.Entities
{
    public sealed class AlertRecipient
    {
        public int AlertRecipientId { get; set; }
        public int AlertId { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public DateTime? NextReminderAt { get; set; }

        public Alert? Alert { get; set; }
        public User? User { get; set; }
    }
}

