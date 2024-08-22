namespace LumenicBackend.Services
{
    public class ToolService : IToolService
    {
        private readonly DatabaseService databaseService;

        public ToolService(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public async Task<List<Tool>> GetToolsByOrganizationId(string organizationId)
        {
            var tools = await this.databaseService.GetToolsByOrganizationId(organizationId);
            return tools;
        }

        public async Task<List<Tool>> GetToolsByAgentId(string agentId)
        {
            var tools = await this.databaseService.GetToolsByAgentId(agentId);
            return tools;
        }

        public async Task AddTool(ToolRequest toolRequest)
        {
            var tool = new Tool()
            {
                Id = Guid.Parse(toolRequest.Id),
                OrganizationId = Guid.Parse(toolRequest.OrganizationId),
                ExecutionFrequency = toolRequest.ExecutionFrequency,
                Name = toolRequest.Name,
                Description = toolRequest.Description,
                Url = toolRequest.Url,
                Structure = toolRequest.Structure,
            };
            await this.databaseService.AddTool(tool);
        }

        public async Task UpdateTool(ToolRequest toolRequest)
        {
            var tool = new Tool()
            {
                Id = Guid.Parse(toolRequest.Id),
                OrganizationId = Guid.Parse(toolRequest.OrganizationId),
                ExecutionFrequency = toolRequest.ExecutionFrequency,
                Name = toolRequest.Name,
                Description = toolRequest.Description,
                Url = toolRequest.Url,
                Structure = toolRequest.Structure,
            };
            await this.databaseService.UpdateTool(tool);
        }

        public async Task DeleteTool(string toolId)
        {
            await this.databaseService.DeleteTool(toolId);
        }
    }
}
