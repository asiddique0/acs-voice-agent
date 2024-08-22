namespace LumenicBackend.Controllers
{
    [Route("api/callLedger")]
    [ApiController]
    public class CallLedgerController : ControllerBase
    {
        private readonly ICallLedgerService callLedgerService;

        public CallLedgerController(ICallLedgerService callLedgerService)
        {
            this.callLedgerService = callLedgerService;
        }

        [HttpPost]
        [Route("getCallLedgersByOrganizationId")]
        public async Task<IActionResult> GetAllCallLedgersByOrganizationId(CallLedgerRequest request)
        {
            var result = callLedgerService.GetCallLedgersByOrganizationId(request.OrganizationId);
            return new JsonResult(result);
        }
    }
}
