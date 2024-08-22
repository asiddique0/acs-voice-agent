namespace LumenicBackend.Services
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly DatabaseService databaseService;
        private readonly IVectorService vectorService;

        public KnowledgeBaseService(DatabaseService databaseService, IVectorService vectorService)
        {
            this.databaseService = databaseService;
            this.vectorService = vectorService;
        }

        public async Task<List<string>> GetAllKnowledgeBaseIndexes()
        {
            var kbIndexes = await this.vectorService.GetAllIndexes();
            return kbIndexes;
        }

        public async Task<List<KnowledgeBase>> GetKnowledgeBasesByOrganizationId(string organizationId)
        {
            var kbs = await this.databaseService.GetKnowledgeBasesByOrganizationId(organizationId);
            return kbs;
        }

        public async Task<UsageResult> AddKnowledgeBase(KnowledgeBaseRequest request)
        {
            var kb = new KnowledgeBase()
            {
                Id = Guid.Parse(request.Id!),
                OrganizationId = Guid.Parse(request.OrganizationId!),
                Name = request.Name!,
                IndexName = request.Index!,
                Content = request.Content!,
            };

            var usage = await this.vectorService.InsertTextIntoVectorDb(
                    request.Index!,
                    request.OrganizationId!,
                    request.Content!);
            await this.databaseService.AddKnowledgeBase(kb);
            return usage;
        }

        public async Task<UsageResult> UpdateKnowledgeBase(KnowledgeBaseRequest request)
        {
            var kb = new KnowledgeBase()
            {
                Id = Guid.Parse(request.Id!),
                OrganizationId = Guid.Parse(request.OrganizationId!),
                Name = request.Name!,
                IndexName = request.Index!,
                Content = request.Content!,
            };

            var usage = await this.vectorService.InsertTextIntoVectorDb(
                    request.Index!,
                    request.OrganizationId!,
                    request.Content!);
            await this.databaseService.UpdateKnowledgeBase(kb);

            return usage;
        }

        public async Task DeleteKnowledgeBase(string index, string organizationId)
        {
            await this.vectorService.DeleteIndex(index, organizationId);
            await this.databaseService.DeleteKnowledgeBaseByIndex(index, organizationId);
        }

        public async Task<List<string>> SearchKnowledgeBase(KnowledgeBaseRequest request)
        {
            (var result, var _) = await this.vectorService.SearchVectorDb(request.Index!, request.OrganizationId!, request.Content!);
            return result;
        }
    }
}
