using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using mySystem.Repositories;
using mySystem.Models;

namespace mySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, IUserRepository userRepository, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { success = false, message = "Username and password are required" });
                }

                if (request.Password.Length < 6)
                {
                    return BadRequest(new { success = false, message = "Password must be at least 6 characters" });
                }

                // Check if user already exists
                bool userExists = await _userRepository.UserExistsAsync(request.Username);
                if (userExists)
                {
                    return BadRequest(new { success = false, message = "Username already exists" });
                }

                // Hash password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email ?? "",
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.CreateUserAsync(user);

                _logger.LogInformation($"New user registered: {request.Username} (ID: {createdUser.Id})");

                return Ok(new
                {
                    success = true,
                    message = "User registered successfully",
                    userId = createdUser.Id,
                    username = createdUser.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering user: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error registering user" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { success = false, message = "Username and password are required" });
                }

                // Get user from database
                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid username or password" });
                }

                // Verify password
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    return Unauthorized(new { success = false, message = "Invalid username or password" });
                }

                // Generate JWT token
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim("UserId", user.Id.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"] ?? "60")),
                    Issuer = jwtSettings["Issuer"],
                    Audience = jwtSettings["Audience"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(secretKey),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation($"User logged in: {user.Username} (ID: {user.Id})");

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    token = tokenString,
                    userId = user.Id,
                    username = user.Username,
                    expiresIn = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging in: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error logging in" });
            }
        }
    }

    // Request models
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}