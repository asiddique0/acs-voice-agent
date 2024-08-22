namespace LumenicBackend.Interfaces
{
    public interface IArtificialIntelligenceProvider
    {
        Task<(CallActions, UsageResult)> DetermineCallAction(List<ChatHistory> history);
        Task<(CallClassification, UsageResult)> DetermineCallClassification(List<ChatHistory> history);
        Task<(bool, UsageResult)> DetermineCallToolsUsage(List<ChatHistory> history, List<Tool> tools);
        Task<(IEnumerable<ToolOutboundResponse>, UsageResult)> ExecuteTools(List<Tool> tools, List<ChatHistory> userMessage);
        Task<(Response<ChatCompletions>, UsageResult)> AnswerAsync(string userQuery, string systemTemplate, string searchIndex, string organizationId, string toolsResult, List<ChatHistory>? history);
    }
}
