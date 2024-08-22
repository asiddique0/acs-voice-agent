using LumenicBackend.Models.Requests;

namespace LumenicBackend.Interfaces
{
    public interface IToolService
    {
        public Task<List<Tool>> GetToolsByOrganizationId(string organizationId);
        public Task<List<Tool>> GetToolsByAgentId(string agentId);
        public Task AddTool(ToolRequest toolRequest);
        public Task UpdateTool(ToolRequest toolRequest);
        public Task DeleteTool(string toolId);
    }
}
