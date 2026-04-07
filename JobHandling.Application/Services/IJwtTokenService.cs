namespace JobHandling.Application.Services
{
    /// <summary>
    /// Abstraction for JWT token generation.
    /// Allows swapping token providers (e.g., switching to an identity server)
    /// without modifying consuming controllers.
    /// </summary>
    public interface IJwtTokenService
    {
        string GenerateToken(string username);
    }
}