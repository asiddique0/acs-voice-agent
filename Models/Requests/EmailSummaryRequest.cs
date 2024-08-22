namespace LumenicBackend.Models.Requests
{
    public class SummaryRequest
    {
        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }
    }
}