namespace LumenicBackend.Models
{
    public class ResourceUsage
    {
        public int SpeechToTextDuration { get; set; }

        public int TextToSpeechCharacters { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string LlmUsageResultsRedisCompat { get; set; }

        public Dictionary<string, UsageResult> LlmUsageResults { get; set; }
    }

    public static class ResourceUsageHelper
    {
        public static ResourceUsage InitializeEmpty()
        {
            var llmUsageResultsEmpty = new Dictionary<string, UsageResult>()
            {
                [StartupConstants.CallClassificationModelName] = UsageResultHelper.GetEmpty(),
                [StartupConstants.CallDetermineActionNeededModelName] = UsageResultHelper.GetEmpty(),
                [StartupConstants.CallToolsInvokerModelName] = UsageResultHelper.GetEmpty(),
                [StartupConstants.CallConversationModelName] = UsageResultHelper.GetEmpty(),
                [StartupConstants.EmbeddingModelName] = UsageResultHelper.GetEmpty(),
                [StartupConstants.GeneralPurposeModelName] = UsageResultHelper.GetEmpty(),
            };

            return new ResourceUsage()
            {
                SpeechToTextDuration = 0,
                TextToSpeechCharacters = 0,
                LlmUsageResults = llmUsageResultsEmpty,
                LlmUsageResultsRedisCompat = JsonConvert.SerializeObject(llmUsageResultsEmpty),
            };
        }
    }

    public class ResourceUsageReport : ResourceUsage
    {
        public ResourceUsageReport(ResourceUsage resourceUsage)
        {
            this.SpeechToTextDuration = resourceUsage.SpeechToTextDuration;
            this.TextToSpeechCharacters = resourceUsage.TextToSpeechCharacters;
            this.LlmUsageResults = resourceUsage.LlmUsageResults;
        }
        public bool Recorded { get; set; }
        public double CallDuration { get; set; }
        public string CallDirection { get; set; }
        public int MessageCount { get; set; }
    }
}
