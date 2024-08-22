namespace LumenicBackend.Services
{
    public class PineconeVectorService : IVectorService
    {
        private readonly ILogger logger;
        private readonly PineconeClient pineconeClient;
        private readonly Index<Pinecone.Grpc.GrpcTransport> pineconeIndex;
        private readonly OpenAIClient openAIClient;
        private string deploymentName;
        private readonly TextSplitter textSplitter;

        public PineconeVectorService(PineconeClient pineconeClient, Index<Pinecone.Grpc.GrpcTransport> index, OpenAIClient openAiClient, string embeddingModelName, ILogger<PineconeVectorService> logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(embeddingModelName);

            this.deploymentName = embeddingModelName;
            this.pineconeClient = pineconeClient;
            this.pineconeIndex = index;
            this.openAIClient = openAiClient;
            this.logger = logger;
            this.textSplitter = new RecursiveCharacterTextSplitter(chunkSize: 1000, chunkOverlap: 50);
        }

        public async Task<List<string>> GetAllIndexes()
        {
            var indexes = await pineconeClient.ListIndexes();

            var result = new List<string>();

            foreach(var index in indexes)
            {
                result.Add(index.Name);
            }

            return result;
        }

        public async Task<UsageResult> InsertTextIntoVectorDb(string index, string organizationId, string text)
        {
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

            var vectors = new List<Pinecone.Vector>();

            for (var i = 0; i < textChunks.Count; i++)
            {
                var chunk = textChunks.ElementAt(i);
                var embeddingItem = embeddings.Value.Data.ElementAt(i);
                var floatArr = embeddingItem.Embedding.ToArray();
                var id = $"{organizationId}:{index}:{i}";

                var vector = new Pinecone.Vector()
                {
                    Id = id,
                    Values = floatArr,
                    Metadata = new MetadataMap()
                    {
                        ["index_name"] = index,
                        ["organization_id"] = organizationId,
                        ["content"] = chunk,
                    },
                };
                vectors.Add(vector);
            }

            await pineconeIndex.Upsert(vectors, indexNamespace: organizationId);

            var usage = new UsageResult()
            {
                CompletionTokens = 0,
                PromptTokens = embeddings.Value.Usage.PromptTokens,
            };

            return usage;
        }

        public async Task<(List<string>, UsageResult)> SearchVectorDb(string index, string organizationId, string text)
        {
            var embeddingOption = new EmbeddingsOptions(this.deploymentName, [text])
            {
                Dimensions = 1536,
            };

            var embedding = await openAIClient.GetEmbeddingsAsync(embeddingOption);

            var embeddingItem = embedding.Value.Data.ElementAt(0);

            var floatArr = embeddingItem.Embedding.ToArray();

            var filter = new MetadataMap()
            {
                ["organization_id"] = new MetadataMap()
                {
                    ["$eq"] = organizationId,
                },
                ["index_name"] = new MetadataMap()
                {
                    ["$eq"] = index,
                },
            };

            var response = await pineconeIndex.Query(floatArr, 3, filter, indexNamespace: organizationId, includeMetadata: true);

            Console.WriteLine($"(DEBUG) Vector Response count: {response.Length}");

            var result = new List<string>();

            foreach(var item in response)
            {
                if (item.Metadata != null && item.Metadata.TryGetValue("content", out var metadataValue))
                {
                    var value = metadataValue.Inner?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        result.Add(value);
                    }
                };
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
            var filter = new MetadataMap()
            {
                ["organization_id"] = new MetadataMap()
                {
                    ["$eq"] = organizationId,
                },
                ["index_name"] = new MetadataMap()
                {
                    ["$eq"] = index,
                },
            };

            await pineconeIndex.Delete(filter, indexNamespace: organizationId);

            return true;
        }
    }
}
