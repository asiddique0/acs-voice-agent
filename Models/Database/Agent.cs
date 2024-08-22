namespace LumenicBackend.Models.Database
{
    public class Agent
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Greeting { get; set; }
        public string VoiceModel { get; set; }
        public string Description { get; set; }
        public string Instructions { get; set; }
        public string SearchIndex { get; set; }
        public bool Recorded { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }

        public ICollection<AgentTool> AgentTools { get; set; }
    }
}
