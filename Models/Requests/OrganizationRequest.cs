namespace LumenicBackend.Models.Requests
{
    public class OrganizationRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    public class OrganizationControllerRequest
    {
        [JsonPropertyName("organizationName")]
        public string? OrganizationName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }
    }
}
