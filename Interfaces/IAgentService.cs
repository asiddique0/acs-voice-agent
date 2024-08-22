using LumenicBackend.Models.Requests;

namespace LumenicBackend.Interfaces
{
    public interface IAgentService
    {
        public Task<List<Agent>> GetAgents(string organizationId);
        public Task AddAgent(AgentRequest agentRequest);
        public Task AddAgentWithTools(AgentToolRequest agentToolRequest);
        public Task AddToolsToAgent(ToolAgentRequest toolAgentRequest);
        public Task UpdateAgent(AgentRequest agentRequest);
        public Task DeleteAgent(string agentId);
    }
}
