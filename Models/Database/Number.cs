namespace LumenicBackend.Models.Database
{
    public class Number
    {
        [Key]
        public Guid Id { get; set; }
        public string NumberValue { get; set; }
        public string TransferNumber { get; set; }
        public int TransferWeight { get; set; }
        public bool Active { get; set; }
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public string TimeZone { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid OrganizationId { get; set; }
        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }
    }
}
