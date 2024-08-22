namespace LumenicBackend.Controllers
{
    [Route("api/organization")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService organizationService;

        public OrganizationController(IOrganizationService organizationService)
        {
            this.organizationService = organizationService;
        }

        [HttpPost]
        [Route("getAllByName")]
        public async Task<IActionResult> GetAllOrganizationsByName(OrganizationControllerRequest request)
        {
            var result = await this.organizationService.GetAllOrganizationsByName(request.OrganizationName!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("getAllByEmail")]
        public async Task<IActionResult> GetAllOrganizationsByEmail(OrganizationControllerRequest request)
        {
            var result = await this.organizationService.GetAllOrganizationsByEmail(request.Email!);
            return new JsonResult(result);
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddOrganization(OrganizationRequest request)
        {
            await this.organizationService.AddOrganization(request);
            return Ok();
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateOrganization(OrganizationRequest request)
        {
            await this.organizationService.UpdateOrganization(request);
            return Ok();
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteOrganization(OrganizationControllerRequest request)
        {
            await this.organizationService.DeleteOrganization(request.OrganizationId!);
            return Ok();
        }
    }
}
