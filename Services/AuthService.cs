using mySystem.DTOs;
using mySystem.Models;
using mySystem.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace mySystem.Services
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterDto request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Username and password are required"
                    };
                }

                if (request.Password.Length < 6)
                {
                    return new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Password must be at least 6 characters"
                    };
                }

                // Check if user exists
                bool userExists = await _userRepository.UserExistsAsync(request.Username);
                if (userExists)
                {
                    return new RegisterResponseDto
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Hash password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email ?? "",
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.CreateUserAsync(user);

                _logger.LogInformation($"New user registered: {request.Username} (ID: {createdUser.Id})");

                return new RegisterResponseDto
                {
                    Success = true,
                    Message = "User registered successfully",
                    UserId = createdUser.Id,
                    Username = createdUser.Username
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering user: {ex.Message}");
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = "Error registering user"
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Username and password are required"
                    };
                }

                // Get user
                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // Verify password
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // Generate token
                var token = GenerateJwtToken(user);

                _logger.LogInformation($"User logged in: {user.Username} (ID: {user.Id})");

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    UserId = user.Id,
                    Username = user.Username,
                    ExpiresIn = int.Parse(_configuration.GetSection("JwtSettings")["ExpirationMinutes"] ?? "60")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging in: {ex.Message}");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Error logging in"
                };
            }
        }

        private string GenerateJwtToken(User user)
        {
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
            return tokenHandler.WriteToken(token);
        }
    }
}