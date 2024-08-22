namespace LumenicBackend.Models.Database
{
    public class AgentTool
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public Guid ToolId { get; set; }

        [ForeignKey("AgentId")]
        public Agent Agent { get; set; }

        [ForeignKey("ToolId")]
        public Tool Tool { get; set; }
    }
}
