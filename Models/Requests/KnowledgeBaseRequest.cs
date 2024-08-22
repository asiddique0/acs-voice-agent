namespace LumenicBackend.Models.Requests
{
    public class KnowledgeBaseRequest
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("organizationId")]
        public string? OrganizationId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("index")]
        public string? Index { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
