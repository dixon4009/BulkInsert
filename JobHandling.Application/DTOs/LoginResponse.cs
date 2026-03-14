using System.Text.Json.Serialization;

namespace JobHandling.Application.DTOs
{
    public class LoginResponse
    {
        /// <summary>
        /// JWT authentication token
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Authenticated username
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }
}