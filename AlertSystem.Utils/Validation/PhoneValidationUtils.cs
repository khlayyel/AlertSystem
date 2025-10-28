using System.Text.RegularExpressions;

namespace AlertSystem.Utils.Validation
{
    /// <summary>
    /// Centralized phone number validation to eliminate duplication across projects
    /// </summary>
    public static class PhoneValidationUtils
    {
        private static readonly Regex PhoneRegex = new Regex(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);
        private static readonly Regex CleanPhoneRegex = new Regex(@"[^\d+]", RegexOptions.Compiled);

        /// <summary>
        /// Validates and cleans a phone number for international format
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <param name="defaultCountryCode">Default country code if not provided (e.g., "+33" for France)</param>
        /// <returns>Cleaned and validated phone number or null if invalid</returns>
        public static string? ValidateAndCleanPhoneNumber(string? phoneNumber, string defaultCountryCode = "+33")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            // Remove all non-digit characters except +
            var cleaned = CleanPhoneRegex.Replace(phoneNumber, "");

            // If it doesn't start with +, add the default country code
            if (!cleaned.StartsWith("+"))
            {
                cleaned = defaultCountryCode + cleaned;
            }

            // Validate the cleaned phone number
            if (PhoneRegex.IsMatch(cleaned))
            {
                return cleaned;
            }

            return null;
        }

        /// <summary>
        /// Validates if a phone number is in correct international format
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            return PhoneRegex.IsMatch(phoneNumber);
        }

        /// <summary>
        /// Formats a phone number for display
        /// </summary>
        /// <param name="phoneNumber">The phone number to format</param>
        /// <returns>Formatted phone number</returns>
        public static string FormatPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            var cleaned = CleanPhoneRegex.Replace(phoneNumber, "");
            
            if (cleaned.Length >= 10)
            {
                // Format as +XX XXX XXX XXXX for display
                if (cleaned.StartsWith("+"))
                {
                    var countryCode = cleaned.Substring(0, 3);
                    var number = cleaned.Substring(3);
                    return $"{countryCode} {number.Substring(0, Math.Min(3, number.Length))} {number.Substring(3)}";
                }
            }

            return phoneNumber;
        }
    }
}
