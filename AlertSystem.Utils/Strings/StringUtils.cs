using System.Text.RegularExpressions;

namespace AlertSystem.Utils.Strings
{
    /// <summary>
    /// String utility functions
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Checks if a string is a valid email address
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True if valid email, false otherwise</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a string is a valid phone number (international format)
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid phone number, false otherwise</returns>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Basic international phone number validation
            var regex = new Regex(@"^\+[1-9]\d{1,14}$");
            return regex.IsMatch(phoneNumber);
        }

        /// <summary>
        /// Truncates a string to specified length and adds ellipsis if needed
        /// </summary>
        /// <param name="input">String to truncate</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>Truncated string</returns>
        public static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;

            return input.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Removes all whitespace from a string
        /// </summary>
        /// <param name="input">String to process</param>
        /// <returns>String without whitespace</returns>
        public static string RemoveWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, @"\s+", "");
        }
    }
}
