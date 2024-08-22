namespace LumenicBackend.Services
{
    public class CallAutomationService : ICallAutomationService
    {
        private readonly CallAutomationClient client;
        private readonly IConfiguration configuration;
        private readonly IIdentityService identityService;
        private readonly IMessageService messageService;
        private readonly ITranscriptionService transcriptionService;
        private readonly IChatService chatService;
        private readonly DatabaseService databaseService;
        private readonly IRedisConnectionProvider redisConnectionProvider;
        private readonly IRedisCollection<CustomOperationContext> contextService;
        private readonly IStorageService storageService;
        private readonly IArtificialIntelligenceProvider deepInfraService;
        private readonly IArtificialIntelligenceProvider openAIService;
        private readonly ILogger logger;

        private readonly string acsEndpoint;
        private readonly string cgsEndpoint;
        private readonly string baseUrl;
        private const string EndCallPhraseToConnectAgent = "Sure, let me transfer you. Thank you for calling, goodbye.";
        private const string EndCall = "Thank you for calling, goodbye.";
        private const string CustomerQueryTimeout = "I'm sorry, I wasn't quite able to hear that.";
        private const string NoResponse = "I didn't hear any input from you.";
        private const string InvalidAudio = "Invalid speech phrase or tone detected.";
        private const string CallTransferMessage = "A call is being transferred to you from {0}. Please follow up with them if no call is received.";
        private const string SsmlTemplate = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"{0}\">{1}</voice></speak>";
        private static HashSet<string> sentenceSeperators = new() { ".", "!", "?", ";", "。", "！", "？", "；", "\n" };

        public CallAutomationService(
            IConfiguration configuration,
            IIdentityService identityService,
            IMessageService messageService,
            ITranscriptionService transcriptionService,
            IChatService chatService,
            DatabaseService databaseService,
            IRedisConnectionProvider redisConnectionProvider,
            IStorageService storageService,
            [FromKeyedServices("DeepInfraService")] IArtificialIntelligenceProvider deepInfraService,
            [FromKeyedServices("OpenAIService")] IArtificialIntelligenceProvider openAIService,
            ILogger<CallAutomationService> logger)
        {
            this.configuration = configuration;
            this.identityService = identityService;
            this.messageService = messageService;
            this.transcriptionService = transcriptionService;
            this.chatService = chatService;
            this.databaseService = databaseService;
            this.redisConnectionProvider = redisConnectionProvider;
            this.storageService = storageService;
            this.deepInfraService = deepInfraService;
            this.openAIService = openAIService;
            this.logger = logger;

            var acsConnectionString = configuration["AcsConnectionString"];
            acsEndpoint = configuration["AcsEndpoint"] ?? string.Empty;
            cgsEndpoint = configuration["AzureAIServiceEndpoint"] ?? string.Empty;
            baseUrl = configuration["HostUrl"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
            ArgumentException.ThrowIfNullOrEmpty(acsEndpoint);
            ArgumentException.ThrowIfNullOrEmpty(cgsEndpoint);
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);

            this.client = new CallAutomationClient(acsConnectionString);
            this.contextService = redisConnectionProvider.RedisCollection<CustomOperationContext>();
        }

        public async Task<CreateCallResult> CreateCallAsync(string botNumber, string userNumber)
        {
            (var botUserId, var botToken) = await identityService.GetNewUserIdAndToken();
            (var userId, var userToken) = await identityService.GetNewUserIdAndToken();

            var chatClientResponse = await chatService.CreateConversation(
                topic: Constants.ConversationTopicName,
                userId: userId,
                token: userToken,
                botUserId: botUserId,
                botToken: botToken);

            var threadId = chatClientResponse.ThreadId!;

            var context = databaseService.InitializeCustomOperationContext(
                    threadId: threadId,
                    botNumber: botNumber,
                    botUserId: botUserId,
                    botToken: botToken,
                    userNumber: userNumber,
                    userId: userId,
                    userToken: userToken,
                    direction: CallDirection.Outbound.ToString().ToLowerInvariant());

            try
            {
                var callbackUri = new Uri(
                    baseUri: new Uri(baseUrl),
                    relativeUri: "/api/callbacks");
                var target = new PhoneNumberIdentifier(userNumber);
                var caller = new PhoneNumberIdentifier(botNumber);
                var callInvite = new CallInvite(target, caller);
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
                {
                    CallIntelligenceOptions = new CallIntelligenceOptions()
                    {
                        CognitiveServicesEndpoint = new Uri(cgsEndpoint),
                    },
                };

                var result = await client.CreateCallAsync(createCallOptions);
                context!.CallConnectionId = result.Value.CallConnection.CallConnectionId;

                // Store call operation context within Redis
                var redisResult = await contextService.InsertAsync(context!, TimeSpan.FromHours(2));
                logger.LogInformation("Redis insert success: {redisResult}", redisResult);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not create outbound call");
                throw;
            }
        }

        public async Task HandleEvent(AcsIncomingCallEventData incomingCallEvent)
        {
            var botNumber = incomingCallEvent.ToCommunicationIdentifier.PhoneNumber.Value;
            var userNumber = incomingCallEvent.FromCommunicationIdentifier.PhoneNumber.Value;

            (var botUserId, var botToken) = await identityService.GetNewUserIdAndToken();
            (var userId, var userToken) = await identityService.GetNewUserIdAndToken();

            var chatClientResponse = await chatService.CreateConversation(
                topic: Constants.ConversationTopicName,
                userId: userId,
                token: userToken,
                botUserId: botUserId,
                botToken: botToken);

            var threadId = chatClientResponse.ThreadId;

            var context = databaseService.InitializeCustomOperationContext(
                    threadId: threadId!,
                    botNumber: botNumber,
                    botUserId: botUserId,
                    botToken: botToken,
                    userNumber: userNumber,
                    userId: userId,
                    userToken: userToken,
                    direction: CallDirection.Inbound.ToString().ToLowerInvariant());

            try
            {
                var options = new AnswerCallOptions(incomingCallEvent.IncomingCallContext, new Uri(baseUrl + "/api/callbacks"))
                {
                    CallIntelligenceOptions = new CallIntelligenceOptions()
                    {
                        CognitiveServicesEndpoint = new Uri(cgsEndpoint),
                    },
                };

                var result = await client.AnswerCallAsync(options);
                context!.CallConnectionId = result.Value.CallConnection.CallConnectionId;

                // Store call operation context within Redis
                var redisResult = await contextService.InsertAsync(context!, TimeSpan.FromHours(2));
                logger.LogInformation("Redis insert success: {redisResult}", redisResult);

                logger.LogInformation("Answered inbound call, callConnectionId={}", result.Value.CallConnection.CallConnectionId);
            }
            catch (Azure.RequestFailedException ex)
            {
                if (!Encoding.UTF8.GetString(ex.GetRawResponse()?.Content?.ToArray()!).Contains("IDX23010"))
                {
                    logger.LogError(ex, $"Could not answer inbound call");
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not answer inbound call");
                throw;
            }
        }

        public async Task HandleEvent(CallConnected callConnected)
        {
            var callConnectionId = callConnected.CallConnectionId;

            var context = await contextService.FindByIdAsync(GetContextKey(callConnectionId));
            context!.ResourceUsage.LlmUsageResults = JsonConvert.DeserializeObject<Dictionary<string, UsageResult>>(context.ResourceUsage.LlmUsageResultsRedisCompat)!;

            var threadId = context!.ThreadId;

            int startHour = context!.StartHour;
            int endHour = context.EndHour;
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);

            bool isInboundCall = context.Direction.Equals(CallDirection.Inbound.ToString(), StringComparison.InvariantCultureIgnoreCase);

            var utcNow = DateTimeOffset.UtcNow;
            DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow.UtcDateTime, timeZone);
            if (isInboundCall && currentTime.Hour >= startHour && currentTime.Hour < endHour)
            {
                double transferWeightProbability = ((double)context.TransferWeight) / 100.0;
                bool transferCall = CallRouterUtil.DetermineIfTransferCall(transferWeightProbability);

                if (transferCall)
                {
                    await TransferCall(context.UserNumber, context.TransferNumber, callConnected.CallConnectionId);
                    logger.LogInformation("Call transferred with ConnectionId: {0}", context.CallConnectionId);

                    context.CallEndReason = CallEndReason.CallTransferredToPerson.ToString().ToLowerInvariant();

                    context.ResourceUsage.LlmUsageResultsRedisCompat = JsonConvert.SerializeObject(context.ResourceUsage.LlmUsageResults);
                    await contextService.UpdateAsync(context);
                    return;
                }
            }

            context!.ServerCallId = callConnected.ServerCallId;

            string greeting = context!.Greeting;
            string botUserId = context!.BotUserId;
            string botToken = context!.BotToken;

            if (context!.Recorded)
            {
                try
                {
                    var recordingResult = await StartRecordingCall(callConnected.ServerCallId, threadId);
                    context.RecordingId = recordingResult.Value.RecordingId;
                }
                catch (Exception ex)
                {
                    logger.LogError("Could not initiate call recording: {0}", ex);
                }
            }

            context.ResourceUsage.TextToSpeechCharacters += greeting.Length;
            context.SpeechToTextStartTime = utcNow.ToUnixTimeSeconds();

            RunTaskInBackground(() => TranscribeBotVoice(greeting, botUserId, botToken, threadId));

            context.ResourceUsage.LlmUsageResultsRedisCompat = JsonConvert.SerializeObject(context.ResourceUsage.LlmUsageResults);
            await contextService.UpdateAsync(context);

            // Play a greeting and start listening for customer input
            var callMedia = GetCallConnection(callConnected.CallConnectionId).GetCallMedia();
            var recognizeOptions = GetMediaRecognizeSpeechOptions(greeting, context.UserNumber, threadId, context.VoiceModel);
            await callMedia.StartRecognizingAsync(recognizeOptions);
        }

        public async Task HandleEvent(PlayCompleted playCompleted)
        {
            Console.WriteLine("(DEBUG) PlayCompleted::START");
            Console.WriteLine("(DEBUG) PlayCompleted::END");
        }

        public async Task HangUp(string callConnectionId)
        {
            var callConnection = GetCallConnection(callConnectionId);
            await callConnection.HangUpAsync(true);
        }

        private async Task<Response<StartRecognizingCallMediaResult>> RecognizeAsyncWithRetries(CallMedia callMedia, CallMediaRecognizeSpeechOptions options, TimeSpan delay, int maxRetry)
        {

            var pipelineOptions = new RetryStrategyOptions<Response<StartRecognizingCallMediaResult>>
            {
                ShouldHandle = new PredicateBuilder<Response<StartRecognizingCallMediaResult>>()
                    .Handle<Azure.RequestFailedException>()
                    .HandleResult(r => r.GetRawResponse().Status == (int)System.Net.HttpStatusCode.BadRequest),
                MaxRetryAttempts = maxRetry,
                Delay = delay
            };

            var pipeline = new ResiliencePipelineBuilder<Response<StartRecognizingCallMediaResult>>()
              .AddRetry(pipelineOptions)
              .Build();

            var response = await pipeline.ExecuteAsync(async res => await callMedia.StartRecognizingAsync(options, res));

            return response;
        }

        public async Task HandleEvent(RecognizeCompleted recognizeCompleted)
        {
            var callMedia = GetCallConnection(recognizeCompleted.CallConnectionId).GetCallMedia();

            var speechResult = (recognizeCompleted.RecognizeResult as SpeechResult)?.Speech ?? string.Empty;
            
            var callConnectionId = recognizeCompleted.CallConnectionId;

            var context = await contextService.FindByIdAsync(GetContextKey(callConnectionId));
            context!.ResourceUsage.LlmUsageResults = JsonConvert.DeserializeObject<Dictionary<string, UsageResult>>(context!.ResourceUsage.LlmUsageResultsRedisCompat)!;

            var utcNow = DateTimeOffset.UtcNow;

            var threadId = context!.ThreadId;

            var voiceModel = context!.VoiceModel;
            var userId = context.UserId;
            var botUserId = context.BotUserId;
            var botToken = context.BotToken;

            var chatHistory = await GetFormattedChatHistory(threadId: threadId, botUserId, botToken) ?? new List<ChatHistory>();

            chatHistory.Append(new()
            {
                SenderDisplayName = "Customer (voice)",
                Content = speechResult,
            });

            RunTaskInBackground(() => TranscribeCustomerVoice(speechResult, userId, botToken, threadId));

            var toolSubset = context?.Tools.Where(x => x.ExecutionFrequency == ToolTypes.CallEveryTime).ToList();

            var areToolsNeeded = false;

            if (toolSubset?.Count > 0)
            {
                (areToolsNeeded, var determineToolsUsage) = await this.deepInfraService.DetermineCallToolsUsage(chatHistory.TakeLast(3).ToList(), toolSubset);

                context!.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.CallDetermineActionNeededModelName, determineToolsUsage.PromptTokens, determineToolsUsage.CompletionTokens);
            }

            var formattedToolsResponse = string.Empty;

            if (areToolsNeeded && toolSubset != null)
            {
                var intermediateResponse = IntermediateResponseGenerator.Generate();

                var playOptions = GetPlaySpeechOptions(intermediateResponse, context!.UserNumber, voiceModel);
                RunTaskInBackground(() => callMedia.PlayAsync(playOptions));

                (var toolsResponse, var toolsUsage) = await this.deepInfraService.ExecuteTools(toolSubset, chatHistory.TakeLast(3).ToList());

                chatHistory.Append(new()
                {
                    SenderDisplayName = "VoiceBot",
                    Content = intermediateResponse,
                });

                RunTaskInBackground(() => TranscribeBotVoice(intermediateResponse, botUserId, botToken, threadId));

                context!.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.CallToolsInvokerModelName, toolsUsage.PromptTokens, toolsUsage.CompletionTokens);

                formattedToolsResponse = FormatToolsResponse(toolsResponse);

                RunTaskInBackground(() => TranscribeToolsBot(formattedToolsResponse, botUserId, botToken, threadId));
            }

            if (context!.SpeechToTextStartTime == null)
            {
                logger.LogError("SpeechToTextStartTime is null for correlationId: {correlationId}", recognizeCompleted.CorrelationId);
                context.SpeechToTextStartTime = utcNow.ToUnixTimeSeconds();
            }

            context.ResourceUsage.SpeechToTextDuration += (int)(utcNow.ToUnixTimeSeconds() - context.SpeechToTextStartTime);

            var systemTemplate = context.SystemTemplate;
            var searchIndex = context.SearchIndex;
            var organizationId = context.OrganizationId;

            (var llmResponse, var searchUsage) = await openAIService.AnswerAsync(speechResult, systemTemplate, searchIndex, organizationId, formattedToolsResponse, chatHistory);

            if (llmResponse.GetRawResponse().IsError)
            {
                (llmResponse, searchUsage) = await deepInfraService.AnswerAsync(speechResult, systemTemplate, searchIndex, organizationId, formattedToolsResponse, chatHistory);
                logger.LogError("OpenAI Response Code: {0}", llmResponse.GetRawResponse().Status);
            }

            context.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.GeneralPurposeModelName, llmResponse.Value.Usage.PromptTokens, llmResponse.Value.Usage.CompletionTokens);
            context.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.EmbeddingModelName, searchUsage.PromptTokens, searchUsage.CompletionTokens);

            var llmResponseContent = llmResponse.Value.Choices[0].Message.Content;

            chatHistory.Append(new()
            {
                SenderDisplayName = "VoiceBot",
                Content = llmResponseContent,
            });

            var recognizeOptions = GetMediaRecognizeSpeechOptions(
                llmResponseContent,
                context!.UserNumber,
                threadId,
                voiceModel);

            RunTaskInBackground(() => RecognizeAsyncWithRetries(callMedia, recognizeOptions, TimeSpan.FromSeconds(1), 10));

            context.SpeechToTextStartTime = utcNow.ToUnixTimeSeconds();

            context.ResourceUsage.TextToSpeechCharacters += llmResponseContent.Length;

            RunTaskInBackground(() => TranscribeBotVoice(llmResponseContent, botUserId, botToken, threadId));

            (var action, var actionUsage) = await deepInfraService.DetermineCallAction(chatHistory.TakeLast(3).ToList());

            context.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.CallDetermineActionNeededModelName, actionUsage.PromptTokens, actionUsage.CompletionTokens);

            if (action == CallActions.Escalate && !string.IsNullOrEmpty(context.TransferNumber))
            {
                var goodbye = EndCallPhraseToConnectAgent;
                var playOptions = GetPlaySpeechOptions(goodbye, context.UserNumber, voiceModel);
                RunTaskInBackground(() => TranscribeBotVoice(goodbye, botUserId, botToken, threadId));
                await callMedia.PlayAsync(playOptions);
                await Task.Delay(5000);
                await TransferCall(context.UserNumber, context.TransferNumber, recognizeCompleted.CallConnectionId);
                logger.LogInformation("Call transferred with ConnectionId: {0}", context.CallConnectionId);

                context.CallEndReason = CallEndReason.CallTransferredToPerson.ToString().ToLowerInvariant();

            }
            else if (action == CallActions.End_Call)
            {
                var goodbye = EndCall;
                var playOptions = GetPlaySpeechOptions(goodbye, context.UserNumber, voiceModel);
                RunTaskInBackground(() => TranscribeBotVoice(goodbye, botUserId, botToken, threadId));
                await callMedia.PlayAsync(playOptions);
                await Task.Delay(5000);
                await HangUp(recognizeCompleted.CallConnectionId);

                logger.LogInformation("Call Ended with ConnectionId: {0}", context.CallConnectionId);

                context!.CallEndReason = CallEndReason.BotHungUp.ToString().ToLowerInvariant();
            }

            logger.LogInformation("(DEBUG) Usage:\n\t\tStartDateTime: {0}\n\t\tSTT StartTime: {1}\n\t\tSTT Duration: {2}\n\t\tTTS Characters: {3}",
                        context.StartDateTime,
                        context.SpeechToTextStartTime,
                        context.ResourceUsage.SpeechToTextDuration,
                        context.ResourceUsage.TextToSpeechCharacters);

            context.ResourceUsage.LlmUsageResultsRedisCompat = JsonConvert.SerializeObject(context.ResourceUsage.LlmUsageResults);
            await contextService.UpdateAsync(context);            
        }

        public async Task HandleEvent(RecognizeFailed recognizeFailedEvent)
        {
            var callConnectionId = recognizeFailedEvent.CallConnectionId;
            var callConnection = this.GetCallConnection(recognizeFailedEvent.CallConnectionId);
            var callMedia = callConnection.GetCallMedia();
            var resultInformation = recognizeFailedEvent.ResultInformation;
            logger.LogError("Encountered error during recognize, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);

            var context = await contextService.FindByIdAsync(GetContextKey(callConnectionId));
            context!.ResourceUsage.LlmUsageResults = JsonConvert.DeserializeObject<Dictionary<string, UsageResult>>(context!.ResourceUsage.LlmUsageResultsRedisCompat)!;

            var utcNow = DateTimeOffset.UtcNow;

            var threadId = context!.ThreadId;

            var botUserId = context!.BotUserId;
            var botToken = context!.BotToken;
            var voiceModel = context.VoiceModel;

            // Do not retry on 401 errors
            if (resultInformation?.Code == 401)
            {
                RunTaskInBackground(() => TranscribeBotVoice("Unable to establish call.", botUserId, botToken, threadId));
                logger.LogError("Unable to use provided cognitive services resource. Please check the linking between communication and cognitive services resource");

                context.CallEndReason = CallEndReason.CallDropped.ToString().ToLowerInvariant();

                var callLedger = await CreateCallLedger(context);

                RunTaskInBackground(() => databaseService.CreateOrUpdateCallLedger(callLedger));

                RunTaskInBackground(() => HangUp(recognizeFailedEvent.CallConnectionId));

                throw new ArgumentException(cgsEndpoint);
            }
            var reasonCode = recognizeFailedEvent.ReasonCode;
            string replyText = reasonCode switch
            {
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizePlayPromptFailed) => CustomerQueryTimeout,
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizeInitialSilenceTimedOut) => NoResponse,
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizeIncorrectToneDetected) => InvalidAudio,
                _ => CustomerQueryTimeout,
            };

            context.ResourceUsage.TextToSpeechCharacters += replyText.Length;

            var recognizeOptions = GetMediaRecognizeSpeechOptions(replyText, context.UserNumber, threadId, voiceModel);
            RunTaskInBackground(() => callMedia.StartRecognizingAsync(recognizeOptions));

            context.SpeechToTextStartTime = utcNow.ToUnixTimeSeconds();

            RunTaskInBackground(() => TranscribeBotVoice(replyText, botUserId, botToken, threadId));

            context.CallEndReason = CallEndReason.CallDropped.ToString().ToLowerInvariant();

            context.ResourceUsage.LlmUsageResultsRedisCompat = JsonConvert.SerializeObject(context.ResourceUsage.LlmUsageResults);
            await contextService.UpdateAsync(context);
        }

        public async Task HandleEvent(PlayFailed playFailedEvent)
        {
            var resultInformation = playFailedEvent.ResultInformation;

            logger.LogError("Encountered error during play, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);

            var callConnectionId = playFailedEvent.CallConnectionId;

            await HangUp(callConnectionId);
        }

        public async Task HandleEvent(AcsRecordingFileStatusUpdatedEventData recordingEventData, string callConnectionId)
        {
            logger.LogInformation("AcsRecordingFileStatusUpdatedEventData::START");
            var recordingChunks = recordingEventData.RecordingStorageInfo.RecordingChunks.ToList();
            var compositeStream = new MemoryStream();
            foreach(var chunk in recordingChunks)
            {
                var recordingUri = new Uri(chunk.ContentLocation);
                var recordingStream = new MemoryStream();

                await client.GetCallRecording().DownloadToAsync(recordingUri, recordingStream);
                
                recordingStream.Position = 0;
                await recordingStream.CopyToAsync(compositeStream);
            }

            compositeStream.Position = 0;
            await this.storageService.StoreFile(callConnectionId, compositeStream);
            logger.LogInformation("AcsRecordingFileStatusUpdatedEventData::END");
        }

        public async Task HandleEvent(CallDisconnected callDisconnectedEvent)
        {
            Console.WriteLine("CallDisconnnected::START");
            var callConnectionId = callDisconnectedEvent.CallConnectionId;

            var context = await contextService.FindByIdAsync(GetContextKey(callConnectionId));
            context!.ResourceUsage.LlmUsageResults = JsonConvert.DeserializeObject<Dictionary<string, UsageResult>>(context.ResourceUsage.LlmUsageResultsRedisCompat)!;

            var callEndReason = context!.CallEndReason;

            context!.CallEndReason = callEndReason.Equals(CallEndReason.Unknown.ToString(), StringComparison.InvariantCultureIgnoreCase)
                                        ? CallEndReason.UserHungUp.ToString().ToLowerInvariant()
                                        : callEndReason;

            var callLedger = await CreateCallLedger(context);

            logger.LogInformation($"(DEBUG) CallLedgerId: {callLedger.Id}; CallConnectionId: {callConnectionId}");

            await databaseService.CreateOrUpdateCallLedger(callLedger);

            await CleanupResources(context!);
            Console.WriteLine("CallDisconnnected::END");
        }

        public async Task HandleEvent(CallEndedEvent callEndedEvent)
        {
            Console.WriteLine("CallEndedEvent::START");
            var durationOfCallInSeconds = callEndedEvent.durationOfCall;
            Console.WriteLine("CallEndedEvent::END");
        }

        private void RunTaskInBackground(Func<Task> taskToRun)
        {
            // Start the task without awaiting it
            var backgroundTask = taskToRun();

            // Handle exceptions
            backgroundTask.ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    // Log the exception or handle it
                    logger.LogError(task.Exception.Flatten().ToString());
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private CallConnection GetCallConnection(string callConnectionId) =>
            client.GetCallConnection(callConnectionId);

        private async Task TranscribeCustomerVoice(string text, string userId, string userToken, string threadId) =>
            await transcriptionService.TranscribeVoiceMessageToChat(
                userId: userId,
                userToken: userToken,
                threadId: threadId,
                displayName: "Customer (voice)",
                text: text);

        private async Task TranscribeBotVoice(string text, string botUserId, string botToken, string threadId) =>
            await transcriptionService.TranscribeVoiceMessageToChat(
                userId: botUserId,
                userToken: botToken,
                threadId: threadId,
                displayName: "VoiceBot",
                text: text);

        private async Task TranscribeToolsBot(string text, string botUserId, string botToken, string threadId) =>
            await transcriptionService.TranscribeVoiceMessageToChat(
                userId: botUserId,
                userToken: botToken,
                threadId: threadId,
                displayName: "ToolsBot",
                text: text);

        private async Task<List<ChatHistory>?> GetFormattedChatHistory(string threadId, string botUserId, string botToken)
        {
            if (string.IsNullOrEmpty(threadId) || string.IsNullOrEmpty(botUserId))
            {
                return null;
            }
            botToken = string.IsNullOrEmpty(botToken) ? await identityService.GetTokenForUserId(botUserId) : botToken;

            var chatClient = new ChatClient(
                endpoint: new Uri(acsEndpoint),
                communicationTokenCredential: new CommunicationTokenCredential(botToken));

            var chatThreadClient = chatClient.GetChatThreadClient(threadId: threadId);

            var chatHistory = ChatHelper.GetChatHistoryWithThreadClient(chatThreadClient);
            return chatHistory.OrderBy(x => x.CreatedOn).ToList();
        }

        private CallMediaRecognizeSpeechOptions GetMediaRecognizeSpeechOptions(string content, string userNumber, string threadId, string voiceModel)
        {
            var ssmlText = string.Format(SsmlTemplate, voiceModel, content);
            var ssmlSource = new SsmlSource(ssmlText);

            var textSource = new TextSource(content, voiceModel);

            PlaySource playSource = content.Length <= 400 ? textSource : ssmlSource;

            logger.LogInformation($"(DEBUG) PlaySource type: {playSource.GetType()}; SsmlText: {ssmlText}");

            var recognizeOptions =
                new CallMediaRecognizeSpeechOptions(targetParticipant: new PhoneNumberIdentifier(userNumber))
                {
                    InitialSilenceTimeout = TimeSpan.FromSeconds(20),
                    Prompt = playSource,
                    EndSilenceTimeout = TimeSpan.FromMilliseconds(500),
                };

            return recognizeOptions;
        }

        private static PlayOptions GetPlaySpeechOptions(string content, string userNumber, string voiceModel)
        {
            var playSource = new TextSource(content, voiceModel);
            return new PlayOptions(
                playSource: playSource,
                playTo: [ new PhoneNumberIdentifier(userNumber) ]);
        }

        private async Task TransferCall(string transfereeNumber, string transferTargetNumber, string callConnectionId)
        {
            var transferDestination = new PhoneNumberIdentifier(transferTargetNumber);
            var transferee = new PhoneNumberIdentifier(transfereeNumber);
            var transferOption = new TransferToParticipantOptions(transferDestination) { Transferee = transferee };
            var callConnection = client.GetCallConnection(callConnectionId);
            await callConnection.TransferCallToParticipantAsync(transferOption);
        }

        public async Task CleanupResources(CustomOperationContext context)
        {
            if (context!.Recorded)
            {
                try
                {
                    RunTaskInBackground(() => StopRecordingCall(context.RecordingId!));
                }
                catch (Exception ex)
                {
                    logger.LogError("Could not stop recording for recording id {0} with exception: {1}", context.RecordingId, ex);
                }
            }

            RunTaskInBackground(() => chatService.DeleteChatThread(context.ThreadId, context!.BotToken!));

            RunTaskInBackground(() => DeleteUserAndBot(context.UserId, context.BotUserId));

            RunTaskInBackground(() => contextService.DeleteAsync(context));
        }

        public async Task<Response<RecordingStateResult>> StartRecordingCall(string serverCallId, string callConnectionId)
        {
            var recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId))
            {
                RecordingContent = RecordingContent.Audio,
                RecordingChannel = RecordingChannel.Mixed,
                RecordingFormat = RecordingFormat.Mp3,
                RecordingStateCallbackUri = new Uri(baseUrl + "/api/events" + $"?callConnectionId={callConnectionId}")
            };

            return await client.GetCallRecording().StartAsync(recordingOptions);
        }

        public async Task<Response> StopRecordingCall(string recordingId)
        {
            return await client.GetCallRecording().StopAsync(recordingId);
        }

        public static string GetContextKey(string callConnectionId) => $"{Constants.CustomOperationContextName}:{callConnectionId}";

        private static string FormatToolsResponse(IEnumerable<ToolOutboundResponse> responses)
        {
            var formattedToolsResponse = new StringBuilder("Results from calling tools:\n");
            foreach (var response in responses)
            {
                formattedToolsResponse.AppendLine($"- {response.Name}: {response.Response}");
            }

            return formattedToolsResponse.ToString();
        }

        private List<TranscriptItem> CleanupTranscriptForStorage(List<ChatHistory> history)
        {
            var cleanedHistory = new List<TranscriptItem>();

            foreach (var historyItem in history)
            {
                cleanedHistory.Add(new TranscriptItem()
                {
                    SenderDisplayName = historyItem.SenderDisplayName!,
                    Content = historyItem.Content!,
                });
            }

            return cleanedHistory;
        }

        private async Task DeleteUserAndBot(string userId, string botUserId)
        {
            await identityService.DeleteUserByUserId(userId);
            await identityService.DeleteUserByUserId(botUserId);
        }

        private async Task<CallLedger> CreateCallLedger(CustomOperationContext context, double durationOfCallInSeconds = -1)
        {
            var utcNow = DateTimeOffset.UtcNow;
            double durationInSeconds = (durationOfCallInSeconds == -1) ? (utcNow.ToUnixTimeSeconds() - context.StartDateTime) : durationOfCallInSeconds;

            logger.LogInformation($"(DEBUG) utcNow: {utcNow.ToUnixTimeSeconds()}; context.StartDateTime: {context.StartDateTime}; context.SpeechToTextStartTime: {context.SpeechToTextStartTime}");

            var transcript = await GetFormattedChatHistory(threadId: context.ThreadId, context.BotUserId, context.BotToken);
            var reducedTranscript = CleanupTranscriptForStorage(transcript!);

            var toolSubset = context.Tools.Where(x => x.ExecutionFrequency == ToolTypes.AfterCall).ToList();

            if (toolSubset?.Count > 0)
            {
                (var toolsResponse, var toolsUsage) = await this.deepInfraService.ExecuteTools(toolSubset, transcript!);
                context.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.CallToolsInvokerModelName, toolsUsage.PromptTokens, toolsUsage.CompletionTokens);

                var formattedToolsResponse = FormatToolsResponse(toolsResponse);

                reducedTranscript.Add(new TranscriptItem()
                {
                    SenderDisplayName = "ToolsBot",
                    Content = formattedToolsResponse,
                });
            }

            (var sentiment, var sentimentUsageResult) = await deepInfraService.DetermineCallClassification(transcript!);
            context.ResourceUsage.LlmUsageResults.IncrementLlmUsageTokens(StartupConstants.CallClassificationModelName, sentimentUsageResult.PromptTokens, sentimentUsageResult.CompletionTokens);

            double telephonyCost = CostCalculator.CalculateTelephonyCost(durationInSeconds, context.Direction);
            double recordingCost = context.Recorded ? CostCalculator.CalculateRecordingCost(durationInSeconds) : 0;
            double messageCost = CostCalculator.CalculateMessagingCost(reducedTranscript.Count);
            double speechToTextCost = CostCalculator.CalculateSpeechToTextCost(context.ResourceUsage.SpeechToTextDuration);
            double textToSpeechCost = CostCalculator.CalculateTextToSpeechCost(context.ResourceUsage.TextToSpeechCharacters);
            (double llmInCost, double llmOutCost) = CostCalculator.CalculateTotalInputAndOutputTokenCost(context.ResourceUsage);

            double totalCost = telephonyCost + recordingCost + messageCost + speechToTextCost + textToSpeechCost + llmInCost + llmOutCost;

            var resourceUsageReport = new ResourceUsageReport(context.ResourceUsage)
            {
                Recorded = context.Recorded,
                CallDuration = durationInSeconds,
                CallDirection = context.Direction,
                MessageCount = transcript!.Count,
            };

            var startDateTime = DateTimeOffset.FromUnixTimeSeconds(context.StartDateTime).UtcDateTime;

            logger.LogInformation($"(DEBUG) startDateTime: {startDateTime}; endDateTime: {utcNow.UtcDateTime}");

            return new CallLedger()
            {
                Id = Guid.Parse(context.CallLedgerId),
                OrganizationId = Guid.Parse(context.OrganizationId),
                ThreadId = context.ThreadId,
                AgentId = Guid.Parse(context.AgentId),
                BotNumber = context.BotNumber,
                UserNumber = context.UserNumber,
                StartDateTime = startDateTime,
                EndDateTime = utcNow.UtcDateTime,
                Duration = durationInSeconds,
                Direction = context.Direction,
                CallEndReason = context.CallEndReason,
                Sentiment = sentiment.ToString().ToLowerInvariant(),
                CreatedAt = utcNow.UtcDateTime,
                TotalCost = totalCost,
                ResourceUsageReport = JsonConvert.SerializeObject(resourceUsageReport),
                Transcript = JsonConvert.SerializeObject(reducedTranscript)
            };
        }
    }
}
