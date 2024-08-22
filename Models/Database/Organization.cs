namespace LumenicBackend.Models.Database
{
    public class Organization
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
