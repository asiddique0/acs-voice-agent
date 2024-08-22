namespace LumenicBackend.Models.Requests
{
    public class AgentRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("greeting")]
        public string Greeting { get; set; }

        [JsonPropertyName("voiceModel")]
        public string VoiceModel { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("instructions")]
        public string Instructions { get; set; }

        [JsonPropertyName("searchIndex")]
        public string SearchIndex { get; set; }

        [JsonPropertyName("recorded")]
        public bool Recorded { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }
    }

    public class AgentControllerRequest
    {
        [JsonPropertyName("agentId")]
        public string? AgentId { get; set; }

        [JsonPropertyName("organizationId")]
        public string? OrganizationId { get; set; }
    }
}
