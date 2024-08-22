namespace LumenicBackend.Controllers
{
    [Route("api/conversation")]
    [ApiController]
    public class SummaryController : ControllerBase
    {
        private readonly ISummaryService summaryService;

        public SummaryController(ISummaryService summaryService)
        {
            this.summaryService = summaryService;
        }

        [HttpPost]
        [Route("emailSummary/sendEmail")]
        public async Task<ActionResult> SendSummaryEmail(SummaryRequest summaryRequest)
        {
            var resultStatus = await summaryService.SendSummaryEmail(summaryRequest);
            var response = new Dictionary<string, string>
            {
                { "result", resultStatus }
            };
            return Ok(response);
        }
    }
}