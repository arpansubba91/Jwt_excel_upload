namespace mySystem.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public int ExpiresIn { get; set; }
    }
}