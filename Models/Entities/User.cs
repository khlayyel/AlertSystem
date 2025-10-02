namespace AlertSystem.Models.Entities
{
    public sealed class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Admin | SuperUser | User
        public int? DepartmentId { get; set; }

        public Department? Department { get; set; }
        public ICollection<AlertRecipient> AlertRecipients { get; set; } = new List<AlertRecipient>();
    }
}

