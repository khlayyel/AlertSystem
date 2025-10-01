namespace AlertSystem.Services
{
    public interface ICurrentUserAccessor
    {
        int? GetUserId();
        string GetRole();
        int? GetDepartmentId();
        bool IsAdmin();
        bool IsSuperUser();
    }
}

