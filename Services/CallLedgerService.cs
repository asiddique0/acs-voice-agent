namespace LumenicBackend.Services
{
    public class CallLedgerService : ICallLedgerService
    {
        private readonly DatabaseService databaseService;

        public CallLedgerService(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public async Task<List<CallLedger>> GetCallLedgersByOrganizationId(string organizationId)
        {
            var callLedgers = await this.databaseService.GetCallLedgersByOrganizationId(organizationId);
            return callLedgers;
        }
    }
}
