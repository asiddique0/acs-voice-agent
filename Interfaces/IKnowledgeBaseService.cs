namespace LumenicBackend.Interfaces
{
    public interface IKnowledgeBaseService
    {
        public Task<List<string>> GetAllKnowledgeBaseIndexes();
        public Task<List<KnowledgeBase>> GetKnowledgeBasesByOrganizationId(string organizationId);
        public Task<UsageResult> AddKnowledgeBase(KnowledgeBaseRequest request);
        public Task<UsageResult> UpdateKnowledgeBase(KnowledgeBaseRequest request);
        public Task DeleteKnowledgeBase(string index, string organizationId);
        public Task<List<string>> SearchKnowledgeBase(KnowledgeBaseRequest request);
    }
}
