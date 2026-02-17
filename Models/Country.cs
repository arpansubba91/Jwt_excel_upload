namespace mySystem.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Capital { get; set; }
        public string Region { get; set; }
        public string Population { get; set; }

        public int UploadedByUserId { get; set; }
        public DateTime UploadDateTime { get; set; }
    }
}