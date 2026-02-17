namespace mySystem.Models
{
    public class Country
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Capital { get; set; }
        public string Region { get; set; }
        public string Population { get; set; }
        public Guid UploadedByUserId { get; set; }
        public DateTime UploadDateTime { get; set; }
    }
}