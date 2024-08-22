namespace LumenicBackend.Services
{
    public class RedisVectorService : IVectorService
    {
        private readonly ILogger logger;
        private readonly IConnectionMultiplexer connectionMultiplexer;
        private readonly OpenAIClient openAIClient;
        private string deploymentName;
        private readonly TextSplitter textSplitter;
        private static Schema schema;

        public RedisVectorService(IConnectionMultiplexer connectionMultiplexer, OpenAIClient openAIClient, IConfiguration configuration, ILogger<RedisVectorService> logger)
        {
            var deploymentName = configuration["OpenAIEmbeddingModelName"] ?? string.Empty;

            ArgumentException.ThrowIfNullOrEmpty(deploymentName);

            this.connectionMultiplexer = connectionMultiplexer;
            this.openAIClient = openAIClient;
            this.deploymentName = deploymentName;
            this.logger = logger;
            this.textSplitter = new RecursiveCharacterTextSplitter(chunkSize: 1000, chunkOverlap: 50);
            this.InitializeSchema();
        }

        private void InitializeSchema()
        {
            schema = new Schema()
                .AddTagField(new FieldName("id"))
                .AddTextField(new FieldName("index_value"))
                .AddTextField(new FieldName("organization_id"))
                .AddTextField(new FieldName("content"))
                .AddVectorField("content_embedding", VectorField.VectorAlgo.HNSW,
                    new Dictionary<string, object>()
                    {
                        ["TYPE"] = "FLOAT32",
                        ["DIM"] = "1536",
                        ["DISTANCE_METRIC"] = "COSINE"
                    }
                );
        }

        private async Task<byte[]> ConvertEmbeddingsIntoByteArray(EmbeddingItem item)
        {
            var floatArr = item.Embedding.ToArray();

            var byteArr = new byte[floatArr.Length * sizeof(float)];
            Buffer.BlockCopy(floatArr, 0, byteArr, 0, byteArr.Length);

            return byteArr;
        }

        private async Task<string> GenerateFinalIndex(string index, string organizationId) => $"{organizationId}_{index}";

        public async Task<List<string>> GetAllIndexes()
        {
            IDatabase db = connectionMultiplexer.GetDatabase();

            SearchCommands ft = db.FT();

            var indexes = await ft._ListAsync();
            
            var result = new List<string>();

            foreach(var index in indexes)
            {
                result.Add(index.ToString());
            }

            return result;
        }

        public async Task<UsageResult> InsertTextIntoVectorDb(string index, string organizationId, string text)
        {
            IDatabase db = connectionMultiplexer.GetDatabase();

            SearchCommands ft = db.FT();

            var finalIndex = await GenerateFinalIndex(index, organizationId);

            await ft.CreateAsync(finalIndex, new FTCreateParams().On(IndexDataType.HASH).Prefix("doc:"), schema);

            var textChunks = new List<string>();

            foreach (var chunk in textSplitter.SplitText(text))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    var chunkEntry = chunk.Replace("\n", " ");
                    textChunks.Add(chunkEntry);
                }
            }

            var embeddingsOptions = new EmbeddingsOptions(this.deploymentName, textChunks)
            {
                Dimensions = 1536,
            };

            var embeddings = await openAIClient.GetEmbeddingsAsync(embeddingsOptions);
            
            if (textChunks.Count != embeddings.Value.Data.Count)
            {
                logger.LogError("Content Chunk Count {0} does not match embeddings count {1} for organization {2} with pineconeIndex {3}", textChunks.Count, embeddings.Value.Data.Count, organizationId, index);
            }

            var batchRedisCommand = db.CreateBatch();

            for(var i = 0; i < textChunks.Count; i++)
            {
                var chunk = textChunks.ElementAt(i);
                var embeddingItem = embeddings.Value.Data.ElementAt(i);
                var byteArr = await ConvertEmbeddingsIntoByteArray(embeddingItem);

                var hashEntry = new HashEntry[]
                {
                    new("index_value", finalIndex),
                    new("organization_id", organizationId),
                    new("content", chunk),
                    new("content_embedding", byteArr),
                };

                db.HashSet($"doc:{finalIndex}:{i}", hashEntry);
            }

            batchRedisCommand.Execute();

            var usage = new UsageResult()
            {
                CompletionTokens = 0,
                PromptTokens = embeddings.Value.Usage.PromptTokens,
            };

            return usage;
        }

        public async Task<(List<string>, UsageResult)> SearchVectorDb(string index, string organizationId, string text)
        {
            var db = connectionMultiplexer.GetDatabase();

            var ft = db.FT();

            var embeddingOption = new EmbeddingsOptions(this.deploymentName, [text]);

            var embedding = await openAIClient.GetEmbeddingsAsync(embeddingOption);

            var embeddingItem = embedding.Value.Data.ElementAt(0);

            var byteArr = await ConvertEmbeddingsIntoByteArray(embeddingItem);

            var finalIndex = await GenerateFinalIndex(index, organizationId);

            var query = new Query("*=>[KNN 3 @content_embedding $query_vec AS score]")
               .AddParam("query_vec", byteArr)
               .ReturnFields("content", "score")
               .SetSortBy("score", ascending: false)
               .Dialect(2);

            var response = await ft.SearchAsync(finalIndex, query);

            var result = new List<string>();

            foreach(var doc in response.Documents)
            {
                var contentProperty = doc.GetProperties().First(x => x.Key == "content");
                var contentValue = contentProperty.Value.ToString();
                result.Add(contentValue);
            }

            var usage = new UsageResult()
            {
                CompletionTokens = 0,
                PromptTokens = embedding.Value.Usage.PromptTokens,
            };

            return (result, usage);
        }

        public async Task<bool> DeleteIndex(string index, string organizationId)
        {
            var db = connectionMultiplexer.GetDatabase();

            var ft = db.FT();

            var finalIndex = await GenerateFinalIndex(index, organizationId);

            return await ft.DropIndexAsync(finalIndex);
        }
    }
}
