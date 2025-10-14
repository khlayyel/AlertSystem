namespace AlertSystem.Models.Entities
{
    public sealed class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } // Utilisé pour WhatsApp aussi
        public string? DesktopDeviceToken { get; set; } // Token pour notifications desktop/web push
        public bool IsActive { get; set; } = true; // Pour désactiver temporairement un utilisateur sans le supprimer
    }
}

