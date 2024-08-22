namespace LumenicBackend.Services
{
    public class ArtificialIntelligenceService : IArtificialIntelligenceProvider
    {
        private readonly ILogger logger;
        private readonly string callActionsModelName;
        private readonly string callClassificationModelName;
        private readonly string toolsModelName;
        private readonly string conversationModelName;
        private readonly OpenAIClient artificialIntelligenceClient;
        private readonly IVectorService vectorService;

        private static readonly HttpClient client = new HttpClient();

        private readonly string CallOptionsSystemPrompt = "You are a sentiment analysis assistant. You help determine when to escalate a call to an agent, end the call, or do nothing. You only reply with one word: escalate, end_call, do_nothing. do_nothing is the default option.";
        private readonly string CallOptionsUserPrompt = "What word (escalate, end_call, do_nothing) does the provided transcript most closely match with:\n{0}";

        private readonly string CallClassificationSystemPrompt = "You are a sentiment analysis assistant. You help determine if a call is either neutral, positive, or negative based on it's transcript. You only reply with one word: neutral, positive, negative. neutral is the default option.";
        private readonly string CallClassificationUserPrompt = "What word (neutral, positive, negative) does the provided transcript most closely match with:\n{0}";

        private readonly string CallToolsUsageSystemPrompt = "You are a sentiment analysis assistant. You help determine whether any of the provided tools need to be used based on the user utterance. You only reply with one word: yes, no. no is the default option.";
        private readonly string CallToolsUsageUserPrompt = "Based on the provided transcript \n{0}\n Do any of the following tools need to be used:\n{1}\nReply only with (yes,no)";

        private readonly string ToolsTemplateSystemPrompt = "You are an assistant who figures out what tools should be called with what properties. Invoke the appropriate tool or tools based on their name and description. The list of tools available to you are numbered below:\n{0}";
        private readonly string ToolsTemplateUserPrompt = "Here is the provided transcript of the conversation:\n{0}";
        private readonly string ToolsInfoPrompt = "The following are the results of tools that were executed prior to this message. Use them to help you with providing a response.\n{0}";

        public ArtificialIntelligenceService(
            string callActionsModelName,
            string callClassificationModelName,
            string toolsModelName,
            string conversationModelName,
            OpenAIClient artificialIntelligenceClient,
            IVectorService vectorService,
            ILogger<ArtificialIntelligenceService> logger)
        {
            this.artificialIntelligenceClient = artificialIntelligenceClient;
            this.vectorService = vectorService;
            this.logger = logger;

            this.callActionsModelName = callActionsModelName;
            this.callClassificationModelName = callClassificationModelName;
            this.toolsModelName = toolsModelName;
            this.conversationModelName = conversationModelName;

            ArgumentException.ThrowIfNullOrEmpty(this.callActionsModelName);
            ArgumentException.ThrowIfNullOrEmpty(this.callClassificationModelName);
            ArgumentException.ThrowIfNullOrEmpty(this.toolsModelName);
            ArgumentException.ThrowIfNullOrEmpty(this.conversationModelName);
        }

        public async Task<(CallActions, UsageResult)> DetermineCallAction(List<ChatHistory> history)
        {
            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                Messages = { },
                MaxTokens = 5,
                ChoiceCount = 1,
                DeploymentName = this.callActionsModelName,
            };

            var formattedTranscript = await GenerateConversationHistoryString(history);

            chatCompletionOptions.Messages.Insert(
                0,
                new ChatRequestSystemMessage(
                    content: CallOptionsSystemPrompt
                ));

            chatCompletionOptions.Messages.Insert(
                1,
                new ChatRequestUserMessage(
                    content: string.Format(CallOptionsUserPrompt, formattedTranscript)
                ));

            var response = await this.artificialIntelligenceClient.GetChatCompletionsAsync(
                chatCompletionsOptions: chatCompletionOptions);

            var responseContent = response.Value.Choices.ElementAt(0).Message.Content.ToLowerInvariant();

            var usage = new UsageResult()
            {
                CompletionTokens = response.Value.Usage.CompletionTokens,
                PromptTokens = response.Value.Usage.PromptTokens,
            };

            if (!string.IsNullOrEmpty(responseContent))
            {
                if (responseContent.Contains("escalate"))
                {
                    return (CallActions.Escalate, usage);
                }
                else if (responseContent.Contains("end_call"))
                {
                    return (CallActions.End_Call, usage);
                }
            }

            return (CallActions.None, usage);

        }

        public async Task<(CallClassification, UsageResult)> DetermineCallClassification(List<ChatHistory> history)
        {
            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                Messages = { },
                MaxTokens = 5,
                ChoiceCount = 1,
                DeploymentName = this.callClassificationModelName,
            };

            chatCompletionOptions.Messages.Insert(
                0,
                new ChatRequestSystemMessage(
                    content: CallClassificationSystemPrompt
                ));

            var chatHistoryFormatted = await GenerateConversationHistoryString(history);

            chatCompletionOptions.Messages.Insert(
                1,
                new ChatRequestUserMessage(
                    content: string.Format(CallClassificationUserPrompt, chatHistoryFormatted)
                ));

            var response = await this.artificialIntelligenceClient.GetChatCompletionsAsync(
                chatCompletionsOptions: chatCompletionOptions);

            var responseContent = response.Value.Choices.ElementAt(0).Message.Content.ToLowerInvariant();

            var usage = new UsageResult()
            {
                CompletionTokens = response.Value.Usage.CompletionTokens,
                PromptTokens = response.Value.Usage.PromptTokens,
            };

            if (!string.IsNullOrEmpty(responseContent))
            {
                if (responseContent.Contains("positive"))
                {
                    return (CallClassification.Positive, usage);
                }
                else if (responseContent.Contains("negative"))
                {
                    return (CallClassification.Negative, usage);
                }
                else if (responseContent.Contains("neutral"))
                {
                    return (CallClassification.Neutral, usage);
                }
            }

            return (CallClassification.Unknown, usage);
        }

        public async Task<(bool, UsageResult)> DetermineCallToolsUsage(List<ChatHistory> history, List<Tool> tools)
        {
            var formattedHistory = await GenerateConversationHistoryString(history);
            var toolsListFormatted = await GenerateToolsListString(tools);

            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                Messages = { },
                MaxTokens = 5,
                ChoiceCount = 1,
                DeploymentName = this.callActionsModelName,
            };

            chatCompletionOptions.Messages.Insert(
                0,
                new ChatRequestSystemMessage(
                    content: CallToolsUsageSystemPrompt
                ));

            chatCompletionOptions.Messages.Insert(
                1,
                new ChatRequestUserMessage(
                    content: string.Format(CallToolsUsageUserPrompt, formattedHistory, toolsListFormatted)
                ));

            var response = await this.artificialIntelligenceClient.GetChatCompletionsAsync(
                chatCompletionsOptions: chatCompletionOptions);

            var responseContent = response.Value.Choices.ElementAt(0)?.Message.Content.ToLowerInvariant();

            var usage = new UsageResult()
            {
                CompletionTokens = response.Value.Usage.CompletionTokens,
                PromptTokens = response.Value.Usage.PromptTokens,
            };

            var result = !string.IsNullOrEmpty(responseContent) && responseContent.Contains("yes");

            return (result, usage);
        }

        private async Task<IEnumerable<ToolOutboundResponse>> MakeRequests(List<ToolOutboundRequest> toolOutboundRequests)
        {
            var tasks = toolOutboundRequests.Select(async request =>
            {
                var content = new StringContent(request.Body, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(request.Url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                logger.LogInformation("Tools Log:\n\t\tRequest URL: {0}\n\t\tRequest Body: {1}\n\t\tResponse Body: {2}\n", request.Url, request.Body, responseBody);

                return new ToolOutboundResponse()
                {
                    Name = request.Name,
                    Response = responseBody,
                };
            });

            return await Task.WhenAll(tasks);
        }

        private async Task<List<ChatCompletionsFunctionToolDefinition>> GenerateToolDefinitions(List<Tool> tools)
        {
            var definitions = new List<ChatCompletionsFunctionToolDefinition>(tools.Count);

            foreach (var tool in tools)
            {
                try
                {
                    var toolDefinition = JsonConvert.DeserializeObject<ToolDefinition>(tool.Structure);

                    if (toolDefinition != null)
                    {
                        var chatCompletionsFunctionToolDefinition = new ChatCompletionsFunctionToolDefinition()
                        {
                            Name = toolDefinition.Name,
                            Description = toolDefinition.Description,
                            Parameters = BinaryData.FromString(JsonConvert.SerializeObject(toolDefinition.Parameters))
                        };

                        definitions.Add(chatCompletionsFunctionToolDefinition);
                    }
                }
                catch (Exception ex) when (ex is Newtonsoft.Json.JsonException || ex is System.Text.Json.JsonException)
                {
                    logger.LogError("Unable to serialize/deserialize tool {0}. ExceptionInfo: {1}", tool.Id, ex);
                }
            }

            return definitions;
        }

        private async Task<List<ToolOutboundRequest>> GenerateToolRequests(IEnumerable<ChatCompletionsToolCall> toolCalls, List<Tool> tools)
        {
            var requests = new List<ToolOutboundRequest>(toolCalls.Count());

            foreach (var toolCall in toolCalls)
            {
                var functionToolCall = toolCall as ChatCompletionsFunctionToolCall;
                var toolDefinition = tools.FirstOrDefault(x => x.Name == functionToolCall?.Name);

                if (!string.IsNullOrEmpty(toolDefinition?.Url) && !string.IsNullOrEmpty(functionToolCall?.Arguments))
                {
                    var toolOutboundRequest = new ToolOutboundRequest()
                    {
                        Name = toolDefinition.Name,
                        Url = toolDefinition.Url,
                        Body = functionToolCall.Arguments,
                    };

                    requests.Add(toolOutboundRequest);
                }
            }

            return requests;
        }

        private async Task<string> GenerateToolsListString(List<Tool> tools)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < tools.Count; i++)
            {
                var tool = tools[i];
                var entry = $"{i+1}. {tool.Name}: {tool.Description}";
                sb.AppendLine(entry);
            }

            return sb.ToString();
        }

        private async Task<string> GenerateConversationHistoryString(List<ChatHistory> history)
        {
            var sb = new StringBuilder();

            foreach(var message in history)
            {
                if (message.SenderDisplayName == "Bot" || message.SenderDisplayName == "VoiceBot")
                {
                    sb.AppendLine($"{ChatRole.Assistant.ToString()}: {message.Content}");
                }
                else if (message.SenderDisplayName == "Customer (voice)")
                {
                    sb.AppendLine($"{ChatRole.User.ToString()}: {message.Content}");
                }
            }

            return sb.ToString();
        }

        public async Task<(IEnumerable<ToolOutboundResponse>, UsageResult)> ExecuteTools(List<Tool> tools, List<ChatHistory> history)
        {
            var formattedToolsList = await GenerateToolsListString(tools);
            var systemPrompt = string.Format(ToolsTemplateSystemPrompt, formattedToolsList);

            var formattedChatHistory = await GenerateConversationHistoryString(history);
            var userPrompt = string.Format(ToolsTemplateUserPrompt, formattedChatHistory);

            var chatCompletionOptions = new ChatCompletionsOptions()
            {
                Temperature = 0,
                Messages = { },
                DeploymentName = this.toolsModelName,
                Tools = { },
            };

            chatCompletionOptions.Messages.Insert(0, new ChatRequestSystemMessage(systemPrompt));

            chatCompletionOptions.Messages.Insert(1, new ChatRequestUserMessage(userPrompt));

            var toolDefinitions = await GenerateToolDefinitions(tools);

            foreach (var toolDefinition in toolDefinitions)
            {
                chatCompletionOptions.Tools.Add(toolDefinition);
            }

            var response = await this.artificialIntelligenceClient.GetChatCompletionsAsync(chatCompletionOptions);

            var chatChoise = response.Value.Choices.FirstOrDefault();

            List<ToolOutboundRequest> toolOutboundRequests = new();

            if (chatChoise != null && chatChoise.FinishReason == CompletionsFinishReason.ToolCalls)
            {
                toolOutboundRequests = await GenerateToolRequests(chatChoise.Message.ToolCalls, tools);
            }

            var usage = new UsageResult()
            {
                CompletionTokens = response.Value.Usage.CompletionTokens,
                PromptTokens = response.Value.Usage.PromptTokens,
            };

            var results = await MakeRequests(toolOutboundRequests);

            return (results, usage);
        }

        private async Task<(string, UsageResult)> SearchForRelevantChunks(string index, string organizationId, string text)
        {
            (var result, var usage) = await this.vectorService.SearchVectorDb(index, organizationId, text);
            return (string.Join("\n", result), usage);
        }

        private async Task<string> ExtractMessageContent(ChatRequestMessage message)
        {
            if (message == null) return string.Empty;

            if (message is ChatRequestSystemMessage systemMsg)
            {
                return systemMsg.Content;
            }
            else if (message is ChatRequestUserMessage userMsg)
            {
                return userMsg.Content;
            }
            else if (message is ChatRequestAssistantMessage assistantMsg)
            {
                return assistantMsg.Content;
            }

            return string.Empty;
        }

        private async Task<UsageResult> GetTokenUsage(IList<ChatRequestMessage> messages)
        {
            const double charToTokensConversionFactor = 0.75;
            const int TokensPerMessage = 3;
            const int TokensPerRole = 1;
            const int BaseTokens = 3;

            int tokenCount = BaseTokens;
            int characterCount = 0;

            foreach(var message in messages)
            {
                tokenCount += TokensPerMessage;
                tokenCount += TokensPerRole;
                characterCount = (await ExtractMessageContent(message)).Length;
            }

            tokenCount += (int)Math.Ceiling(characterCount * charToTokensConversionFactor);

            return new UsageResult()
            {
                PromptTokens = tokenCount,
                CompletionTokens = 0,
            };
        }

        private async Task LogOutboundRequest(ChatCompletionsOptions options, string response)
        {
            var logMessage = "\n";
            foreach (var message in options.Messages)
            {
                if (message is ChatRequestSystemMessage systemMessage)
                {
                    logMessage += $"{systemMessage.Role} : {systemMessage.Content}";
                } 
                else if (message is ChatRequestUserMessage userMessage)
                {
                    logMessage += $"{userMessage.Role} : {userMessage.Content}";
                }
                else if (message is ChatRequestAssistantMessage assistantMessage)
                {
                    logMessage += $"{assistantMessage.Role} : {assistantMessage.Content}";
                }
                logMessage += "\n";
            }
            logMessage += $"assistant: {response}\n";
            logger.LogInformation(logMessage);
        }

        public async Task<(Response<ChatCompletions>, UsageResult)> AnswerAsync(
           string userQuery,
           string systemTemplate,
           string searchIndex,
           string organizationId,
           string toolsResult,
           List<ChatHistory>? history = null)
        {
            history ??= new List<ChatHistory>();

            (var matchingDocs, var searchUsage) = string.IsNullOrEmpty(searchIndex) ? (string.Empty, UsageResultHelper.GetEmpty()) : await SearchForRelevantChunks(searchIndex, organizationId, userQuery);

            matchingDocs = string.IsNullOrEmpty(matchingDocs) ? string.Empty : $"Knowledge Base:\n {matchingDocs}";

            var toolsInfo = string.IsNullOrEmpty(toolsResult) ? string.Empty : string.Format(ToolsInfoPrompt, toolsResult);

            var systemPrompt = string.Format(systemTemplate, toolsInfo, matchingDocs);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = { },
                MaxTokens = 256,
                ChoiceCount = 1,
                DeploymentName = this.conversationModelName,
                Temperature = 1,
            };

            chatCompletionsOptions.Messages.Insert(
                0,
                new ChatRequestSystemMessage(
                    content: systemPrompt
                ));

            foreach (var message in history)
            {
                if (message.SenderDisplayName == "Bot" || message.SenderDisplayName == "VoiceBot")
                {
                    chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(message.Content));
                }
                else if (message.SenderDisplayName == "ToolsBot")
                {
                    // chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(message.Content));
                }
                else
                {
                    chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(message.Content));
                }
            }

            chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(userQuery));

            var response = await this.artificialIntelligenceClient.GetChatCompletionsAsync(chatCompletionsOptions);

            await LogOutboundRequest(chatCompletionsOptions, response.Value.Choices[0].Message.Content);

            return (response, searchUsage);
        }

    }
}
