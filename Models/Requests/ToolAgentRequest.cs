namespace LumenicBackend.Models.Requests
{
    public class ToolAgentRequest
    {
        [JsonPropertyName("agentId")]
        public string AgentId { get; set; }

        [JsonPropertyName("toolIds")]
        public string[] ToolIds { get; set; }
    }
}
