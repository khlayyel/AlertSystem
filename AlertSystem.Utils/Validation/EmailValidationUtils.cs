using System.Text.RegularExpressions;

namespace AlertSystem.Utils.Validation
{
    /// <summary>
    /// Centralized email validation to eliminate duplication across projects
    /// </summary>
    public static class EmailValidationUtils
    {
        // RFC 5322 compliant email regex (simplified but robust)
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Validates if an email address is in correct format
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Check length constraints
            if (email.Length > 254) // RFC 5321 limit
                return false;

            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Validates and normalizes an email address
        /// </summary>
        /// <param name="email">The email address to validate and normalize</param>
        /// <returns>Normalized email address or null if invalid</returns>
        public static string? ValidateAndNormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var normalized = email.Trim().ToLowerInvariant();

            if (IsValidEmail(normalized))
            {
                return normalized;
            }

            return null;
        }

        /// <summary>
        /// Validates multiple email addresses
        /// </summary>
        /// <param name="emails">The email addresses to validate</param>
        /// <returns>Dictionary of email -> validation result</returns>
        public static Dictionary<string, bool> ValidateMultipleEmails(IEnumerable<string> emails)
        {
            var results = new Dictionary<string, bool>();
            
            foreach (var email in emails)
            {
                results[email] = IsValidEmail(email);
            }

            return results;
        }

        /// <summary>
        /// Gets valid emails from a collection
        /// </summary>
        /// <param name="emails">The email addresses to filter</param>
        /// <returns>Collection of valid email addresses</returns>
        public static IEnumerable<string> GetValidEmails(IEnumerable<string> emails)
        {
            return emails.Where(IsValidEmail);
        }
    }
}
