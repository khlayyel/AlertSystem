namespace AlertSystem.Worker.Models
{
    public class UserModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? DesktopDeviceToken { get; set; }
        public bool IsActive { get; set; }
    }
}
