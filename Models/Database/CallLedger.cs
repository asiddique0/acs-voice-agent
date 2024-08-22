namespace LumenicBackend.Models.Database
{
    public class CallLedger
    {
        [Key]
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string ThreadId { get; set; }
        public Guid AgentId { get; set; }
        public string BotNumber { get; set; }
        public string UserNumber { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double Duration { get; set; }
        public string Direction { get; set; }
        public string Sentiment { get; set; }
        public string CallEndReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public double TotalCost { get; set; }
        public string ResourceUsageReport { get; set; }
        public string Transcript { get; set; }
    }
}
