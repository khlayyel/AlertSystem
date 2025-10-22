namespace AlertSystem.Utils.Validation
{
    /// <summary>
    /// Validation utility functions
    /// </summary>
    public static class ValidationUtils
    {
        /// <summary>
        /// Validates that a value is not null or empty
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="parameterName">Name of the parameter for error message</param>
        /// <exception cref="ArgumentException">Thrown if value is null or empty</exception>
        public static void ValidateNotNullOrEmpty(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Parameter '{parameterName}' cannot be null or empty", parameterName);
        }

        /// <summary>
        /// Validates that a value is not null
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value to validate</param>
        /// <param name="parameterName">Name of the parameter for error message</param>
        /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
        public static void ValidateNotNull<T>(T? value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Validates that a number is positive
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="parameterName">Name of the parameter for error message</param>
        /// <exception cref="ArgumentException">Thrown if value is not positive</exception>
        public static void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentException($"Parameter '{parameterName}' must be positive", parameterName);
        }
    }
}
