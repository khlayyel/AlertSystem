using AlertSystem.Entities.Entities;

namespace AlertSystem.Entities.Entities
{
    public sealed class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Alerte> Alertes { get; set; } = new List<Alerte>();
    }
}

