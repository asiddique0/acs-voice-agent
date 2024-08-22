namespace LumenicBackend.Models.Requests
{
    public class AgentToolRequest
    {
        [JsonPropertyName("agent")]
        public AgentRequest Agent { get; set; }

        [JsonPropertyName("tools")]
        public string[] ToolIds { get; set; }
    }
}
