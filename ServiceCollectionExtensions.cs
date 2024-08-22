namespace LumenicBackend.Extensions;

internal static class ServiceCollectionExtensions
{
    public static TokenCredential CreateDelegatedToken(string token)
    {
        AccessToken accessToken = new AccessToken(token, DateTimeOffset.Now.AddDays(180.0));
        return DelegatedTokenCredential.Create((TokenRequestContext _, CancellationToken _) => accessToken);
    }

    public static void AddConfigurations(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var (callClassificationModelName, callDetermineActionNeededModelName, callToolsInvokerModelName, callConversationModelName) =
            (config["CallClassificationModelName"], config["CallActionsModelName"], config["ToolsModelName"], config["ConversationModelName"]);
        var embeddingModelName = config["OpenAIEmbeddingModelName"] ?? string.Empty;
        var generalModelName = config["OpenAIGeneralModelName"] ?? string.Empty;

        ArgumentException.ThrowIfNullOrEmpty(callClassificationModelName);
        ArgumentException.ThrowIfNullOrEmpty(callDetermineActionNeededModelName);
        ArgumentException.ThrowIfNullOrEmpty(callToolsInvokerModelName);
        ArgumentException.ThrowIfNullOrEmpty(callConversationModelName);
        ArgumentException.ThrowIfNullOrEmpty(embeddingModelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(generalModelName);

        StartupConstants.CallClassificationModelName = callClassificationModelName;
        StartupConstants.CallDetermineActionNeededModelName = callDetermineActionNeededModelName;
        StartupConstants.CallToolsInvokerModelName = callToolsInvokerModelName;
        StartupConstants.CallConversationModelName = callConversationModelName;
        StartupConstants.EmbeddingModelName = embeddingModelName;
        StartupConstants.GeneralPurposeModelName = generalModelName;
    }

    public static IServiceCollection AddBackendServices(this IServiceCollection services)
    {
        services.AddSingleton<PineconeClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var pineconeApiKey = config["PineconeApiKey"] ?? string.Empty;

            ArgumentException.ThrowIfNullOrEmpty(pineconeApiKey);

            var client = new PineconeClient(pineconeApiKey);
            return client;
        });

        services.AddSingleton<Index<Pinecone.Grpc.GrpcTransport>>(sp =>
        {
            var client = sp.GetRequiredService<PineconeClient>();

            var index = client.GetIndex(Constants.PineconeIndexName).Result;

            return index;
        });

        services.AddSingleton<IVectorService, PineconeVectorService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            var openAIApiKey = config["OpenAIApiKey"] ?? string.Empty;
            var modelName = StartupConstants.EmbeddingModelName;

            ArgumentException.ThrowIfNullOrEmpty(openAIApiKey);

            var openAIClient = new OpenAIClient(openAIApiKey: openAIApiKey);

            var pineconeClient = sp.GetRequiredService<PineconeClient>();
            var pineconeIndex = sp.GetRequiredService<Index<Pinecone.Grpc.GrpcTransport>>();
            var logger = sp.GetRequiredService<ILogger<PineconeVectorService>>();

            return new PineconeVectorService(pineconeClient, pineconeIndex, openAIClient, modelName, logger);
        });

        services.AddKeyedSingleton<OpenAIClient>("OpenAIClient", (sp, key) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            
            var openAIApiKey = config["OpenAIApiKey"];

            ArgumentException.ThrowIfNullOrEmpty(openAIApiKey);

            var client = new OpenAIClient(openAIApiKey: openAIApiKey);

            return client;
        });

        services.AddKeyedSingleton<OpenAIClient>("DeepInfraClient", (sp, key) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var (deepInfraServiceEndpoint, deepInfraApiKey) =
                (config["DeepInfraServiceEndpoint"], config["DeepInfraApiKey"]);

            ArgumentException.ThrowIfNullOrEmpty(deepInfraServiceEndpoint);
            ArgumentException.ThrowIfNullOrEmpty(deepInfraApiKey);

            var options = new OpenAIClientOptions();

            var client = new OpenAIClient(new Uri(deepInfraServiceEndpoint), CreateDelegatedToken(deepInfraApiKey), options);

            Type type = typeof(OpenAIClient);

            FieldInfo? field = type.GetField("_isConfiguredForAzureOpenAI", BindingFlags.NonPublic | BindingFlags.Instance);

            field?.SetValue(client, false);

            return client;
        });

        services.AddKeyedSingleton<IArtificialIntelligenceProvider, ArtificialIntelligenceService>("OpenAIService", (sp, key) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            
            var openAIModelName = StartupConstants.GeneralPurposeModelName;

            var openAIClient = sp.GetKeyedService<OpenAIClient>("OpenAIClient");
            var vectorService = sp.GetRequiredService<IVectorService>();
            var logger = sp.GetRequiredService<ILogger<ArtificialIntelligenceService>>();

            return new ArtificialIntelligenceService(
                openAIModelName,
                openAIModelName,
                openAIModelName,
                openAIModelName,
                openAIClient!,
                vectorService,
                logger);
        });

        services.AddKeyedSingleton<IArtificialIntelligenceProvider, ArtificialIntelligenceService>("DeepInfraService", (sp, key) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var deepInfraClient = sp.GetKeyedService<OpenAIClient>("DeepInfraClient");
            var vectorService = sp.GetRequiredService<IVectorService>();
            var logger = sp.GetRequiredService<ILogger<ArtificialIntelligenceService>>();

            return new ArtificialIntelligenceService(
                StartupConstants.CallDetermineActionNeededModelName,
                StartupConstants.CallClassificationModelName,
                StartupConstants.CallToolsInvokerModelName,
                StartupConstants.CallConversationModelName,
                deepInfraClient!,
                vectorService,
                logger);
        });

        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConfig = new RedisConnectionConfiguration()
            {
                Host = config["RedisHost"]!,
                Port = int.TryParse(config["RedisPort"], out int parsedPort) ? parsedPort : 6379,
                Password = config["RedisPassword"]!
            };

            ArgumentException.ThrowIfNullOrEmpty(redisConfig.Host);
            ArgumentNullException.ThrowIfNull(redisConfig.Port);
            ArgumentException.ThrowIfNullOrEmpty(redisConfig.Password);

            return new RedisConnectionProvider(redisConfig);
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConfig = new RedisConnectionConfiguration()
            {
                Host = config["RedisHost"]!,
                Port = int.TryParse(config["RedisPort"], out int parsedPort) ? parsedPort : 6379,
                Password = config["RedisPassword"]!
            };

            ArgumentException.ThrowIfNullOrEmpty(redisConfig.Host);
            ArgumentNullException.ThrowIfNull(redisConfig.Port);
            ArgumentException.ThrowIfNullOrEmpty(redisConfig.Password);

            return ConnectionMultiplexer.Connect(redisConfig.ToStackExchangeConnectionString());
        });

        services.AddHostedService<IndexCreationService>();

        services.AddSingleton<BlobContainerClient>(sp => {
            var config = sp.GetRequiredService<IConfiguration>();
            var storageConnectionString = config["AzureStorageConnectionString"];

            ArgumentException.ThrowIfNullOrEmpty(storageConnectionString);

            var blobContainerClient = new BlobContainerClient(storageConnectionString, Constants.RecordingContainerName);

            blobContainerClient.CreateIfNotExists();

            return blobContainerClient;
        });

        services.AddSingleton<IStorageService, StorageService>();

        services.AddDbContextPool<LumenicDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            });
        });

        services.AddScoped<DatabaseService>(sp =>
        {
            return new DatabaseService(sp.GetRequiredService<LumenicDbContext>(), sp.GetRequiredService<ILogger<DatabaseService>>());
        });

        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IIdentityService, IdentityService>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddScoped<INumberService, NumberService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IToolService, ToolService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<ICallLedgerService, CallLedgerService>();
        services.AddScoped<ICallAutomationService, CallAutomationService>();
        return services;
    }
}