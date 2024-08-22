namespace LumenicBackend.Interfaces
{
    public interface IVectorService
    {
        public Task<List<string>> GetAllIndexes();
        public Task<UsageResult> InsertTextIntoVectorDb(string index, string organizationId, string text);
        public Task<(List<string>, UsageResult)> SearchVectorDb(string index, string organizationId, string text);
        public Task<bool> DeleteIndex(string index, string organizationId);
    }
}
