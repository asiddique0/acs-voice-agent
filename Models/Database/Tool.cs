namespace LumenicBackend.Models.Database
{
    public class Tool
    {
        [Key]
        public Guid Id { get; set; }
        public string ExecutionFrequency { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Structure { get; set; }
        public Guid OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }

        public ICollection<AgentTool> AgentTools { get; set; }
    }
}
