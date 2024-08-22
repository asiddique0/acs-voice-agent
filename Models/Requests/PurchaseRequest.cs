namespace LumenicBackend.Models.Requests
{
    public class PurchaseRequest
    {
        [JsonPropertyName("searchId")]
        public string SearchId { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("transferNumber")]
        public string TransferNumber { get; set; }

        [JsonPropertyName("transferWeight")]
        public int TransferWeight { get; set; }

        [JsonPropertyName("startHour")]
        public int StartHour { get; set; }

        [JsonPropertyName("endHour")]
        public int EndHour { get; set; }

        [JsonPropertyName("timezone")]
        public string TimeZone { get; set; }
    }
}
