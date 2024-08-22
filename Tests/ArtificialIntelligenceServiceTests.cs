using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LumenicBackend.Tests
{
    [TestClass]
    public class ArtificialIntelligenceServiceTests
    {
        private OpenAIClient DeepInfraClient;
        private string SystemTemplate;
        private static HashSet<string> sentenceSeperators = new() { ".", "!", "?", ";", "。", "！", "？", "；", "\n" };
        private string ToolsTemplate = "You are an assistant who figures out what tools should be called with what properties. The list of tools available to you are numbered below:\n{0}\n Invoke the appropriate tool or tools based on their name and description. You can extract the properties from the conversation history provided:\n{1}";

        public ArtificialIntelligenceServiceTests()
        {
            this.DeepInfraClient = new OpenAIClient(new Uri("https://api.deepinfra.com/v1/openai"), ServiceCollectionExtensions.CreateDelegatedToken("USTveSHbhy61jC3UAc0NJv7bouMKWjGy"));
            Type type = typeof(OpenAIClient);
            FieldInfo? field = type.GetField("_isConfiguredForAzureOpenAI", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(DeepInfraClient, false);

            this.SystemTemplate = File.ReadAllText("D:\\Documents\\Development\\voice-agent-backend\\Static\\SystemTemplate.txt");
        }

        [TestMethod]
        public async Task SimulateToolsCall()
        {
            var toolDefinition = TestTools.GetTestToolOneDefinition();
            var availableTools = $"1. {toolDefinition.Name}: {toolDefinition.Description}\n";
            var conversationHistory = """
            assistant: Hello, my name is Alyssa Shannon.
            user: Hey Alysaa, I'm calling because I wanted to learn more about solar panels and whether it makes sense.
            assistant: Sure, we can certainly do that! Are there any specific reasons you're looking to install panels that I can address?
            """;

            var systemPrompt = string.Format(ToolsTemplate, availableTools, conversationHistory);
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = { },
                DeploymentName = "meta-llama/Meta-Llama-3-70B-Instruct",
                Tools = { },
            };

            chatCompletionsOptions.Messages.Insert(
                0,
                new ChatRequestSystemMessage(
                    content: systemPrompt
                ));

            chatCompletionsOptions.Messages.Insert(
                1,
                new ChatRequestUserMessage(
                    content: "Please figure out which tools need to be called with which properties and their values."
                ));

            var parameters = JsonConvert.SerializeObject(toolDefinition.Parameters);

            Console.WriteLine($"Parameters: {parameters}");

            var chatCompletionsFunctionToolDefinition = new ChatCompletionsFunctionToolDefinition()
            {
                Name = "TestToolOne",
                Description = toolDefinition.Description,
                Parameters = BinaryData.FromString(parameters),
            };

            chatCompletionsOptions.Tools.Add(chatCompletionsFunctionToolDefinition);

            var response = await this.DeepInfraClient.GetChatCompletionsAsync(chatCompletionsOptions);

            var chatChoise = response.Value.Choices.FirstOrDefault();

            foreach (var toolCall in chatChoise!.Message.ToolCalls)
            {
                var functionCall = toolCall as ChatCompletionsFunctionToolCall;
                Console.WriteLine($"Tool Call Name: {functionCall.Name}, Arguments: {functionCall.Arguments}");
            }
            ;
        }

        [TestMethod]
        public async Task StreamLLMResponse()
        {
            var systemPrompt = string.Format(SystemTemplate, string.Empty, string.Empty);
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = { },
                MaxTokens = 200,
                ChoiceCount = 1,
                DeploymentName = "mistralai/Mixtral-8x7B-Instruct-v0.1",
            };

            chatCompletionsOptions.Messages.Insert(
                0,
                new ChatRequestSystemMessage(
                    content: systemPrompt
                ));

            chatCompletionsOptions.Messages.Insert(
                1,
                new ChatRequestUserMessage(
                    content: "Hello, I'm calling in to learn about what panels options you guys have."
                ));

            var response = await this.DeepInfraClient.GetChatCompletionsStreamingAsync(chatCompletionsOptions);

            var responseSb = new StringBuilder();
            var llmBuffer = new StringBuilder();

            await foreach (var messageChunk in response)
            {
                var content = messageChunk.ContentUpdate;

                Console.WriteLine($"RAW content stream chunk: {content}");

                responseSb.Append(content);

                var finishReason = messageChunk.FinishReason;
                if (finishReason != null &&
                    (finishReason == CompletionsFinishReason.Stopped || finishReason == CompletionsFinishReason.TokenLimitReached) &&
                    llmBuffer.Length > 0)
                {
                    var sentence = llmBuffer.ToString();
                    llmBuffer.Clear();

                    Console.WriteLine($"(DEBUG) RecognizeAsync sentence: {sentence}");
                }
                else
                {
                    if (content != null && sentenceSeperators.Any(content.Contains))
                    {
                        var sentence = llmBuffer.ToString() + content;
                        llmBuffer.Clear();

                        Console.WriteLine($"(DEBUG) RecognizeAsync final sentence: {sentence}");
                    }
                    else
                    {
                        llmBuffer.Append(content);
                    }
                }
            }
            Console.WriteLine($"(DEBUG) Final Response: {responseSb.ToString()}");
        }
    }
}
