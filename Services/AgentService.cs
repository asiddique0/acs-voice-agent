namespace LumenicBackend.Services
{
    public class AgentService : IAgentService
    {
        private readonly DatabaseService databaseService;

        public AgentService(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public async Task<List<Agent>> GetAgents(string organizationId)
        {
            var agents = this.databaseService.GetAgentsByOrganizationId(organizationId);
            return agents;
        }

        public async Task AddAgent(AgentRequest agentRequest)
        {
            var agent = new Agent()
            {
                Id = Guid.Parse(agentRequest.Id),
                Name = agentRequest.Name,
                PhoneNumber = agentRequest.PhoneNumber,
                Greeting = agentRequest.Greeting,
                VoiceModel = agentRequest.VoiceModel,
                Description = agentRequest.Description,
                Instructions = agentRequest.Instructions,
                SearchIndex = agentRequest.SearchIndex,
                Recorded = agentRequest.Recorded,
                OrganizationId = Guid.Parse(agentRequest.OrganizationId),
            };

            await this.databaseService.AddAgent(agent);
        }

        public async Task AddAgentWithTools(AgentToolRequest agentToolRequest)
        {
            var agentRequest = agentToolRequest.Agent;

            var agent = new Agent()
            {
                Id = Guid.Parse(agentRequest.Id),
                Name = agentRequest.Name,
                PhoneNumber = agentRequest.PhoneNumber,
                Greeting = agentRequest.Greeting,
                VoiceModel = agentRequest.VoiceModel,
                Description = agentRequest.Description,
                Instructions = agentRequest.Instructions,
                SearchIndex = agentRequest.SearchIndex,
                Recorded = agentRequest.Recorded,
                OrganizationId = Guid.Parse(agentRequest.OrganizationId),
            };

            await this.databaseService.AddAgent(agent);
            await this.databaseService.AddToolsToAgent(agentRequest.Id, agentToolRequest.ToolIds.ToList());
        }

        public async Task AddToolsToAgent(ToolAgentRequest toolAgentRequest)
        {
            await this.databaseService.AddToolsToAgent(toolAgentRequest.AgentId, toolAgentRequest.ToolIds.ToList());
        }

        public async Task UpdateAgent(AgentRequest agentRequest)
        {
            var agent = new Agent()
            {
                Id = Guid.Parse(agentRequest.Id),
                Name = agentRequest.Name,
                PhoneNumber = agentRequest.PhoneNumber,
                Greeting = agentRequest.Greeting,
                VoiceModel = agentRequest.VoiceModel,
                Description = agentRequest.Description,
                Instructions = agentRequest.Instructions,
                SearchIndex = agentRequest.SearchIndex,
                Recorded = agentRequest.Recorded,
                OrganizationId = Guid.Parse(agentRequest.OrganizationId)
            };

            await this.databaseService.UpdateAgent(agent);
        }

        public async Task DeleteAgent(string agentId)
        {
            await this.databaseService.DeleteAgent(agentId);
        }
    }
}
