namespace LumenicBackend.Common
{
    public static class Constants
    {
        public const string ConversationTopicName = "Call Conversation";
        public const string CustomOperationContextName = "CustomOperationContext";
        public const string RecordingContainerName = "recordings";
        public const string PineconeIndexName = "voice-agent";
    }

    public static class StartupConstants
    {
        public static string CallClassificationModelName { get; set; }
        public static string CallDetermineActionNeededModelName {  get; set; }
        public static string CallToolsInvokerModelName { get; set; }
        public static string CallConversationModelName { get; set; }
        public static string EmbeddingModelName { get; set; }
        public static string GeneralPurposeModelName { get; set; }
    }

    public static class ToolTypes
    {
        public static string CallEveryTime = "every_time";
        public static string AfterCall = "after_call";
    }
}
