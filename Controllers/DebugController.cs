namespace LumenicBackend.Controllers
{
    [Route("api/debug")]
    [ApiController]
    /* These API routes are for developer debug purposes. Not used by sample webapp */
    public class DebugController : Controller
    {
        private readonly ICallAutomationService callAutomationService;
        private readonly IConfiguration configuration;
        private readonly IMessageService messageService;

        public DebugController(
            ICallAutomationService callAutomationService,
            IMessageService messageService,
            IConfiguration configuration)
        {
            this.callAutomationService = callAutomationService;
            this.messageService = messageService;
            this.configuration = configuration;
        }

        [HttpPost]
        [Route("health")]
        public async Task<IActionResult> Health()
        {
            return Ok();
        }

        [HttpPost]
        [Route("callToPstn")]
        public async Task<IActionResult> CreateCall(CallRequest callRequest)
        {
            return Ok(await callAutomationService.CreateCallAsync(callRequest.BotNumber, callRequest.UserNumber));
        }

        [HttpPost]
        [Route("sendSms")]
        public async Task<IActionResult> SendSms(SmsRequest smsRequest)
        {
            return Ok(await messageService.SendTextMessage(smsRequest.senderNumber, smsRequest.targetNumber, smsRequest.message));
        }
    }
}