using JobHandling.Application.DTOs;
using JobHandling.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobHandling.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly JwtTokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(JwtTokenService tokenService, ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="request">Login credentials (username and password)</param>
        /// <returns>JWT token if credentials are valid</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for user: {Username}", request?.Username ?? "unknown");

            // Validate request
            if (request == null || string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login failed: Invalid request - username or password is empty");
                return BadRequest(new ErrorResponse
                {
                    TraceId = HttpContext.TraceIdentifier,
                    Message = "Username and password are required",
                    ErrorType = "ValidationError",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            // Simple demo authentication (in production, use database and proper hashing)
            if (request.Username == "admin" && request.Password == "password")
            {
                try
                {
                    var token = _tokenService.GenerateToken(request.Username);

                    var response = new LoginResponse
                    {
                        Token = token,
                        Username = request.Username,
                        ExpiresIn = 3600
                    };

                    _logger.LogInformation("Login successful for user: {Username}", request.Username);
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating token for user: {Username}", request.Username);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ErrorResponse
                        {
                            TraceId = HttpContext.TraceIdentifier,
                            Message = "An error occurred while generating the token",
                            ErrorType = ex.GetType().Name,
                            StatusCode = StatusCodes.Status500InternalServerError
                        });
                }
            }

            _logger.LogWarning("Login failed: Invalid credentials for user: {Username}", request.Username);
            return Unauthorized(new ErrorResponse
            {
                TraceId = HttpContext.TraceIdentifier,
                Message = "Invalid username or password",
                ErrorType = "AuthenticationError",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }
    }
}