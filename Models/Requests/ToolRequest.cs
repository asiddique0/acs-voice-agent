namespace LumenicBackend.Models.Requests
{
    public class ToolRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("toolExecutionFrequency")]
        public string ExecutionFrequency { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("structure")]
        public string Structure { get; set; }
    }

    public class ToolControllerRequest
    {
        [JsonPropertyName("organizationId")]
        public string? OrganizationId { get; set; }

        [JsonPropertyName("agentId")]
        public string? AgentId { get; set; }

        [JsonPropertyName("toolId")]
        public string? ToolId { get; set; }
    }
}
