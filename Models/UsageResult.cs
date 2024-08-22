namespace LumenicBackend.Models
{
    public class UsageResult
    {
        public int CompletionTokens { get; set; }
        public int PromptTokens { get; set; }
    }

    public static class UsageResultHelper
    {
        public static UsageResult GetEmpty()
        {
            return new UsageResult
            {
                CompletionTokens = 0,
                PromptTokens = 0,
            };
        }
    }
}
