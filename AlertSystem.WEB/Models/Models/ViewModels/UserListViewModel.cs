using AlertSystem.Models.Entities;

namespace AlertSystem.Models.ViewModels
{
    public sealed class UserListViewModel
    {
        public IReadOnlyList<User> Items { get; set; } = Array.Empty<User>();
        public IReadOnlyList<Department> Departments { get; set; } = Array.Empty<Department>();
        public string? Role { get; set; }
        public int? DepartmentId { get; set; }
        public string? Query { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalCount { get; set; }
        public string? SortBy { get; set; }
        public bool Desc { get; set; }
    }
}

