namespace LumenicBackend.Models.Requests
{
    public class SmsRequest
    {
        [JsonPropertyName("senderNumber")]
        public string senderNumber { get; set; }

        [JsonPropertyName("targetNumber")]
        public string targetNumber { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }
    }
}
