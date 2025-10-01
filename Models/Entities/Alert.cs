namespace AlertSystem.Models.Entities
{
    public sealed class Alert
    {
        public int AlertId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty; // Information | Obligatoire
        public bool IsManual { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? DepartmentId { get; set; }

        public Department? Department { get; set; }
        public ICollection<AlertRecipient> Recipients { get; set; } = new List<AlertRecipient>();
    }
}

