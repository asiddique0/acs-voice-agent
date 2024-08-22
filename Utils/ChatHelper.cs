namespace LumenicBackend.Utils
{
    public class ChatHelper
    {
        public static List<ChatHistory> GetChatHistoryWithThreadClient(ChatThreadClient chatThreadClient)
        {
            var chatMessages = chatThreadClient.GetMessages();
            List<ChatHistory> chatHistoryList = new();
            foreach (var chatMessage in chatMessages)
            {
                if (chatMessage.Sender?.RawId is not null)
                {
                    var createdDateTime = JsonConvert.DeserializeObject<DateTime>(chatMessage.Metadata["CreatedDateTime"]);
                    ChatHistory chatHistory = new()
                    {
                        MessageId = chatMessage.Id,
                        Content = chatMessage.Content?.Message,
                        SenderId = chatMessage.Sender?.RawId,
                        CreatedOn = new DateTimeOffset(createdDateTime),
                        MessageType = "chat",
                        ContentType = chatMessage.Type.ToString(),
                        SenderDisplayName = !string.IsNullOrEmpty(chatMessage.SenderDisplayName) ? chatMessage.SenderDisplayName : "Bot",
                    };
                    chatHistoryList.Add(chatHistory);
                }
            }

            return chatHistoryList;
        }
    }
}