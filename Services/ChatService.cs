namespace LumenicBackend.Services
{
    /*
     * This class is not being used in the current implementation other than the CreateConversation method.
     */
    public class ChatService : IChatService
    {
        private readonly IArtificialIntelligenceProvider deepInfraService;
        private readonly IIdentityService identityService;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        private readonly string acsEndpoint;
        private const string PSTNRegex = @"(\+\d{1,3}[-.\s]??\d{10}|\d{3}[-.\s]??\d{3}[-.\s]??\d{4}|\(\d{3}\)[-.\s]??\d{3}[-.\s]??\d{4})";

        public ChatService(
            [FromKeyedServices("DeepInfraService")] IArtificialIntelligenceProvider deepInfraService,
            IIdentityService identityService,
            IConfiguration configuration,
            ILogger<ChatService> logger)
        {
            this.deepInfraService = deepInfraService;
            this.identityService = identityService;
            this.configuration = configuration;
            this.logger = logger;
            this.acsEndpoint = this.configuration["AcsEndpoint"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(acsEndpoint);
        }

        public async Task<ChatClientResponse> CreateConversation(string topic, string userId, string token, string botUserId, string botToken)
        {
            (var chatThreadClient, var threadId) = await GetOrCreateBotChatThreadClient(botUserId, botToken, topic);

            chatThreadClient.AddParticipant(new ChatParticipant(new CommunicationUserIdentifier(userId))
            {
                DisplayName = "Customer"
            });

            return new ChatClientResponse
            {
                ThreadId = threadId,
                Token = token,
                Identity = userId,
                EndpointUrl = acsEndpoint,
                BotUserId = botUserId,
            };
        }

        public async Task<Response> DeleteChatThread(string threadId, string botToken)
        {
            var chatClient = new ChatClient(new Uri(acsEndpoint), new CommunicationTokenCredential(botToken));
            return await chatClient.DeleteChatThreadAsync(threadId);
        }

        public async Task<ChatClientResponse> CreateCustomerConversation(string configId)
        {

            // 0. Get configuration using configId from configuration table
            var topic = string.Empty;

            // 1. Create and cache new identity for customer if needed
            (string userId, string token) = await identityService.GetNewUserIdAndToken();

            // 2. Create and cache new identity for bot if needed
            (string botUserId, string botToken) = await identityService.GetNewUserIdAndToken();

            // 3. Prepare new chat conversation as bot
            (var chatThreadClient, var threadId) = await GetOrCreateBotChatThreadClient(botUserId, botToken, topic);

            // 4. Create/Update the call_ledger with the new userId and token using configId

            // 5. Invite customer to conversation with bot
            chatThreadClient.AddParticipant(new ChatParticipant(new CommunicationUserIdentifier(userId))
            {
                DisplayName = "Customer"
            });

            return new ChatClientResponse
            {
                ThreadId = threadId,
                Token = token,
                Identity = userId,
                EndpointUrl = acsEndpoint,
                BotUserId = botUserId,
            };
        }

        public async Task HandleEvent(AcsChatMessageReceivedInThreadEventData chatEvent)
        {
            var eventSender = chatEvent.SenderCommunicationIdentifier.RawId;
            var eventMessage = chatEvent.MessageBody;
            var eventThreadId = chatEvent.ThreadId;
            var eventSenderType = chatEvent.Metadata.GetValueOrDefault("SenderType");

            string threadId = string.Empty;
            string botUserId = string.Empty;
            string botToken = string.Empty;
            string outboundCallerId = string.Empty;

            string systemTemplate = string.Empty;
            string searchIndex = string.Empty;
            
            // Check if the threadId entry is in active state
            if (eventThreadId != threadId)
            {
                return; // only respond to active thread
            }

            if (eventSender == botUserId)
            {
                return; // don't respond to bot own messages
            }

            if (eventSenderType != null && eventSenderType.Equals("call", StringComparison.OrdinalIgnoreCase))
            {
                return; // don't respond to call transcript messages
            }

            if (eventSenderType != null && eventSenderType.Equals("bot", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            (var chatThreadClient, _) = await GetOrCreateBotChatThreadClient(botUserId, botToken, threadId: eventThreadId);

            // 1. Handle handoff to voice call
            // Currently handoff is detected if message body contains a phone number
            // For more accurate results, it could be sent to OpenAI for analysis
            if (TryGetPhoneNumber(eventMessage, out var phoneNumber))
            {
                // If outbound phonenumber is not configured, fail gracefully
                if (string.IsNullOrEmpty(outboundCallerId))
                {
                    var errorMsg = new SendChatMessageOptions()
                    {
                        Content = "I'm unable to call you, my developer hasn't enabled outbound calls",
                        MessageType = ChatMessageType.Text
                    };
                    errorMsg.Metadata.Add("SenderType", "bot");
                    await chatThreadClient.SendMessageAsync(errorMsg);
                    return;
                }

                var sendChatMessageOptions = new SendChatMessageOptions()
                {
                    Content = "Thank you, I'm calling you now, and you can close this chat if you'd like.",
                    MessageType = ChatMessageType.Text
                };
                sendChatMessageOptions.Metadata.Add("SenderType", "bot");
                await chatThreadClient.SendMessageAsync(sendChatMessageOptions);
            }
            // 2. Respond with openAI generated response
            else
            {
                //(string chatGptResponse, UsageResult _) = await deepInfraService.AnswerAsync(eventMessage, "Jenny", systemTemplate, searchIndex, string.Empty, GetFormattedChatHistory(chatThreadClient));
                //var sendChatMessageOptions = new SendChatMessageOptions()
                //{
                //    Content = chatGptResponse,
                //    MessageType = ChatMessageType.Content
                //};
                //sendChatMessageOptions.Metadata.Add("SenderType", "bot");
                //await chatThreadClient.SendMessageAsync(chatGptResponse, ChatMessageType.Content);
            }
        }

        public async Task<List<ChatHistory>> GetChatHistory(string botUserId, string botToken, string threadId)
        {
            (var chatThreadClient, _) = await GetOrCreateBotChatThreadClient(botUserId, botToken, threadId);
            return GetFormattedChatHistory(chatThreadClient);
        }

        private List<ChatHistory> GetFormattedChatHistory(ChatThreadClient chatThreadClient)
        {
            List<ChatHistory> chatHistoryList = ChatHelper.GetChatHistoryWithThreadClient(chatThreadClient);
            return chatHistoryList.OrderBy(x => x.CreatedOn).ToList();
        }

        private async Task<(ChatThreadClient, string)> GetOrCreateBotChatThreadClient(string botUserId, string botToken = "", string topic = "Customer Support", string threadId = "")
        {
            var botTokenValue = string.IsNullOrEmpty(botToken) ? await identityService.GetTokenForUserId(botUserId) : botToken;

            ChatClient chatClient = new ChatClient(new Uri(acsEndpoint), new CommunicationTokenCredential(botTokenValue));

            string chatThreadId = threadId;

            if (string.IsNullOrEmpty(chatThreadId))
            {
                var botParticipant = new ChatParticipant(new CommunicationUserIdentifier(id: botUserId))
                {
                    DisplayName = "Bot"
                };
                CreateChatThreadResult createChatThreadResult = await chatClient.CreateChatThreadAsync(
                    topic: topic,
                    [ botParticipant ]);
                chatThreadId = createChatThreadResult.ChatThread.Id;
            }

            return (chatClient.GetChatThreadClient(chatThreadId), chatThreadId);
        }

        private static bool TryGetPhoneNumber(string message, out string phoneNumber)
        {
            Regex regex = new(PSTNRegex);
            MatchCollection matches = regex.Matches(message);
            if (matches.Count > 0)
            {
                phoneNumber = matches[0].Value;
                if (!phoneNumber.StartsWith("+"))
                {
                    phoneNumber = $"+{phoneNumber}";
                }
                phoneNumber = phoneNumber.Replace(" ", string.Empty);
                phoneNumber = phoneNumber.Replace("-", string.Empty);
                return true;
            }
            phoneNumber = string.Empty;
            return false;
        }
    }
}