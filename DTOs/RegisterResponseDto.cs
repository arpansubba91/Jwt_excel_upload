namespace mySystem.DTOs
{
    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
    }
}