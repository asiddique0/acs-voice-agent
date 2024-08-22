namespace LumenicBackend.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = [Constants.CustomOperationContextName] )]
    public class CustomOperationContext
    {
        [RedisIdField]
        [Indexed]
        public string CallConnectionId { get; set; }

        [Indexed]
        public string ServerCallId { get; set; }

        [Indexed]
        public string CallLedgerId { get; set; }

        [Indexed]
        public string ThreadId { get; set; }

        public string OrganizationId { get; set; }

        public string AgentId { get; set; }

        public string BotUserId { get; set; }

        public string BotNumber { get; set; }

        public string BotToken { get; set; }

        public string UserId { get; set; }

        public string UserNumber { get; set; }

        public string UserToken { get; set; }

        public string Greeting { get; set; }

        public string VoiceModel { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Topic { get; set; }

        public string SystemTemplate { get; set; }

        public string SearchIndex { get; set; }

        public bool Recorded { get; set; }

        public string? RecordingId { get; set; }

        public string Direction { get; set; }

        public string CallEndReason { get; set; }

        public string TransferNumber { get; set; }

        public int TransferWeight { get; set; }

        public int StartHour { get; set; }

        public int EndHour { get; set; }

        public string TimeZone { get; set; }

        public long StartDateTime { get; set; }

        public long? SpeechToTextStartTime { get; set; }

        public ResourceUsage ResourceUsage { get; set; }

        public List<Tool> Tools { get; set; }
    }
}
