namespace LumenicBackend.Interfaces
{
    public interface IChatService
    {
        Task<ChatClientResponse> CreateConversation(string topic, string userId, string token, string botUserId, string botToken);

        Task<Response> DeleteChatThread(string threadId, string botToken);

        Task<ChatClientResponse> CreateCustomerConversation(string configId);

        Task<List<ChatHistory>> GetChatHistory(string botUserId, string botToken, string threadId);

        Task HandleEvent(AcsChatMessageReceivedInThreadEventData chatMessageReceivedEvent);
    }
}