namespace LumenicBackend.Controllers
{
    [Route("api/tool")]
    [ApiController]
    public class ToolController : ControllerBase
    {
        private readonly IToolService toolService;
        public ToolController(IToolService toolService)

        {
            this.toolService = toolService;
        }

        [HttpPost]
        [Route("getToolsByOrganizationId")]
        public async Task<IActionResult> GetToolsByOrganizationId(ToolControllerRequest request)
        {
            var result = await this.toolService.GetToolsByOrganizationId(request.OrganizationId!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("getToolsByAgentId")]
        public async Task<IActionResult> GetToolsByAgentId(ToolControllerRequest request)
        {
            var result = await this.toolService.GetToolsByAgentId(request.AgentId!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddTool(ToolRequest request)
        {
            await this.toolService.AddTool(request);
            return Ok();
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateTool(ToolRequest request)
        {
            await this.toolService.UpdateTool(request);
            return Ok();
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteTool(ToolControllerRequest request)
        {
            await this.toolService.DeleteTool(request.ToolId!);
            return Ok();
        }
    }
}
