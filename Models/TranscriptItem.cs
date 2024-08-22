namespace LumenicBackend.Models
{
    public class TranscriptItem
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("senderDisplayName")]
        public string SenderDisplayName { get; set; }

    }
}
