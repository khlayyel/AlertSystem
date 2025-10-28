using System.Text.Json;

namespace AlertSystem.Utils.WhatsApp
{
    /// <summary>
    /// Centralized WhatsApp error handling to eliminate duplication across projects
    /// </summary>
    public static class WhatsAppErrorHandler
    {
        /// <summary>
        /// Parses WhatsApp API error response and extracts error details
        /// </summary>
        /// <param name="errorResponse">The error response from WhatsApp API</param>
        /// <returns>Parsed error information</returns>
        public static WhatsAppErrorInfo ParseWhatsAppError(string errorResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(errorResponse);
                var root = document.RootElement;

                var error = root.GetProperty("error");
                var code = error.TryGetProperty("code", out var codeElement) ? codeElement.GetInt32() : 0;
                var message = error.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "Unknown error" : "Unknown error";
                var type = error.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? "Unknown" : "Unknown";

                return new WhatsAppErrorInfo
                {
                    Code = code,
                    Message = message,
                    Type = type,
                    Explanation = GetErrorExplanation(code, message)
                };
            }
            catch (JsonException)
            {
                return new WhatsAppErrorInfo
                {
                    Code = -1,
                    Message = "Failed to parse error response",
                    Type = "ParseError",
                    Explanation = "The error response from WhatsApp API could not be parsed."
                };
            }
        }

        /// <summary>
        /// Gets human-readable explanation for WhatsApp error codes
        /// </summary>
        /// <param name="code">The error code</param>
        /// <param name="message">The error message</param>
        /// <returns>Human-readable explanation</returns>
        public static string GetErrorExplanation(int code, string message)
        {
            return code switch
            {
                100 => "Invalid parameter: One or more parameters in the request are invalid.",
                131000 => "Message undeliverable: The message could not be delivered to the recipient.",
                131021 => "Recipient cannot be messaged: The recipient has not opted in to receive messages from this business.",
                131026 => "Message failed to send: The message could not be sent due to a temporary issue.",
                131047 => "Re-engagement message: The recipient is outside the 24-hour messaging window and needs to send a message first.",
                131051 => "Unsupported message type: The message type is not supported.",
                131052 => "Media download error: The media file could not be downloaded.",
                131053 => "Media upload error: The media file could not be uploaded.",
                131054 => "Template message error: There was an error with the template message.",
                131056 => "Template not found: The specified template does not exist or is not approved.",
                131057 => "Template language not supported: The template language is not supported.",
                131058 => "Template parameter error: There was an error with the template parameters.",
                190 => "Access token expired: The access token has expired and needs to be refreshed.",
                368 => "Temporary issue: WhatsApp API is experiencing temporary issues.",
                80007 => "Rate limit exceeded: Too many requests have been made to the API.",
                _ when message.Contains("phone number") => "Invalid phone number format. Please ensure the phone number is in international format (e.g., +33123456789).",
                _ when message.Contains("template") => "Template message error. Please check if the template is approved and parameters are correct.",
                _ when message.Contains("media") => "Media upload/download error. Please check if the media file is valid and accessible.",
                _ => $"WhatsApp API error: {message}"
            };
        }

        /// <summary>
        /// Determines if an error is retryable
        /// </summary>
        /// <param name="errorInfo">The error information</param>
        /// <returns>True if the error is retryable, false otherwise</returns>
        public static bool IsRetryableError(WhatsAppErrorInfo errorInfo)
        {
            return errorInfo.Code switch
            {
                131026 => true,  // Message failed to send (temporary)
                368 => true,     // Temporary issue
                80007 => true,   // Rate limit (retry after delay)
                _ => false
            };
        }

        /// <summary>
        /// Gets the recommended retry delay for retryable errors
        /// </summary>
        /// <param name="errorInfo">The error information</param>
        /// <returns>Recommended retry delay in seconds</returns>
        public static int GetRetryDelaySeconds(WhatsAppErrorInfo errorInfo)
        {
            return errorInfo.Code switch
            {
                131026 => 30,    // Message failed to send
                368 => 60,       // Temporary issue
                80007 => 300,    // Rate limit (5 minutes)
                _ => 30          // Default retry delay
            };
        }
    }

    /// <summary>
    /// WhatsApp error information structure
    /// </summary>
    public class WhatsAppErrorInfo
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}
