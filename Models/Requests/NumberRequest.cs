namespace LumenicBackend.Models.Requests
{
    public class NumberRequest
    {
        [JsonPropertyName("organizationId")]
        public string? OrganizationId { get; set; }

        [JsonPropertyName("areaCode")]
        public string? AreaCode { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
    }
}
