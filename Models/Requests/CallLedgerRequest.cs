namespace LumenicBackend.Models.Requests
{
    public class CallLedgerRequest
    {
        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }
    }
}
