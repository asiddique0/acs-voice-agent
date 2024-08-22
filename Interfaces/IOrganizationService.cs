using LumenicBackend.Models.Requests;

namespace LumenicBackend.Interfaces
{
    public interface IOrganizationService
    {
        public Task<List<Organization>> GetAllOrganizations();
        public Task<List<Organization>> GetAllOrganizationsByName(string organizationName);
        public Task<List<Organization>> GetAllOrganizationsByEmail(string email);
        public Task AddOrganization(OrganizationRequest organizationRequest);
        public Task UpdateOrganization(OrganizationRequest organizationRequest);
        public Task DeleteOrganization(string organizationId);

    }
}
