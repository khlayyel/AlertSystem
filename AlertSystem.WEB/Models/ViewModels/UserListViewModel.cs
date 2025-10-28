using AlertSystem.Entities.Entities;

namespace AlertSystem.Models.ViewModels
{
    public sealed class UserListViewModel
    {
        public IReadOnlyList<DefUtilisateur> Items { get; set; } = Array.Empty<DefUtilisateur>();
        // Department entity removed in refactoring
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

