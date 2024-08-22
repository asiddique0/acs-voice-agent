namespace LumenicBackend.Controllers
{
    [Route("api/agent")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService agentService;

        public AgentController(IAgentService agentService)
        {
            this.agentService = agentService;
        }

        [HttpPost]
        [Route("getAllByOrganizationId")]
        public async Task<IActionResult> GetAllAgentsByOrganizationId(AgentControllerRequest request)
        {
            var result = await this.agentService.GetAgents(request.OrganizationId!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddAgent(AgentRequest request)
        {
            await this.agentService.AddAgent(request);
            return Ok();
        }

        [HttpPost]
        [Route("addAgentWithTools")]
        public async Task<IActionResult> AddAgentWithTools(AgentToolRequest request)
        {
            await this.agentService.AddAgentWithTools(request);
            return Ok();
        }

        [HttpPost]
        [Route("addToolsToAgent")]
        public async Task<IActionResult> AddToolsToAgent(ToolAgentRequest request)
        {
            await this.agentService.AddToolsToAgent(request);
            return Ok();
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateAgent(AgentRequest request)
        {
            await this.agentService.UpdateAgent(request);
            return Ok();
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteAgent(AgentControllerRequest request)
        {
            await this.agentService.DeleteAgent(request.AgentId!);
            return Ok();
        }
    }
}