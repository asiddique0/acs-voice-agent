namespace LumenicBackend.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly DatabaseService databaseService;

        public OrganizationService(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public async Task<List<Organization>> GetAllOrganizations()
        {
            return await databaseService.GetAllOrganizations();
        }

        public async Task<List<Organization>> GetAllOrganizationsByName(string organizationName)
        {
            return await databaseService.GetAllActiveOrganizationsByName(organizationName);
        }

        public async Task<List<Organization>> GetAllOrganizationsByEmail(string email)
        {
            return await databaseService.GetAllOrganizationsByEmail(email);
        }

        public async Task AddOrganization(OrganizationRequest organizationRequest)
        {
            var organization = new Organization()
            {
                Id = Guid.Parse(organizationRequest.Id),
                Name = organizationRequest.Name,
                Email = organizationRequest.Email,
                CreatedAt = DateTime.UtcNow,
                Active = true,
            };

            await databaseService.AddOrganization(organization);
        }

        public async Task UpdateOrganization(OrganizationRequest organizationRequest)
        {
            var organization = new Organization()
            {
                Id = Guid.Parse(organizationRequest.Id),
                Name = organizationRequest.Name,
                Email = organizationRequest.Email,
                Active = true,
            };

            await databaseService.UpdateOrganization(organization);
        }

        public async Task DeleteOrganization(string organizationId)
        {
            var organization = new Organization()
            {
                Id = Guid.Parse(organizationId),
                Active = false,
            };
            await this.databaseService.UpdateOrganization(organization);
        }
    }
}
