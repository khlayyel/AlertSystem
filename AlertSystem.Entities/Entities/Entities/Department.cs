namespace AlertSystem.Models.Entities
{
    public sealed class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}

