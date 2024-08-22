namespace LumenicBackend.Interfaces
{
    public interface ICallLedgerService
    {
        public Task<List<CallLedger>> GetCallLedgersByOrganizationId(string organizationId);
    }
}
