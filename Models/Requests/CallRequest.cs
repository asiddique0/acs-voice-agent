namespace LumenicBackend.Models.Requests
{
    public class CallRequest
    {
        [JsonPropertyName("botNumber")]
        public string BotNumber { get; set; }

        [JsonPropertyName("userNumber")]
        public string UserNumber { get; set; }
    }
}
