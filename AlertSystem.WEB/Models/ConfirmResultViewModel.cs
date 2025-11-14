namespace AlertSystem.WEB.Models
{
    using AlertSystem.Entities.Entities;

    public sealed class ConfirmResultViewModel
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public Alerte? Alert { get; init; }
        public int ConfirmedCount { get; init; }
        public string ConfirmationTime { get; init; } = string.Empty;
    }
}
