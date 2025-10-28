using System;

namespace AlertSystem.Entities.Entities
{
    public sealed class AlertSendLog
    {
        public int AlertSendLogId { get; set; }
        public int AlerteId { get; set; }
        public string Channel { get; set; } = string.Empty; // Email, WhatsApp, Desktop
        public string Recipient { get; set; } = string.Empty; // email/phone/userId
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public Alerte? Alerte { get; set; }
    }
}


