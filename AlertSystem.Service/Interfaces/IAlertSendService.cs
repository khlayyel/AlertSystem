namespace AlertSystem.Service.Interfaces
{
    /// <summary>
    /// Interface simplifiée pour le service d'envoi d'alertes - respecte le principe ISP
    /// </summary>
    public interface IAlertSendService
    {
        /// <summary>
        /// Envoie une alerte existante par son AlertRecordId
        /// </summary>
        Task<bool> SendExistingAlertAsync(int alertRecordId, string[] emails, string[] phones, string[] desktop);

        /// <summary>
        /// Envoie une nouvelle alerte
        /// </summary>
        Task<bool> SendAlertAsync(string title, string description, int alertTypeId, string[] emails, string[] phones, string[] desktop);
    }
}