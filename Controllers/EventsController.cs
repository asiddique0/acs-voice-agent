namespace LumenicBackend.Controllers
{
    [Route("api")]
    [ApiController]
    public class EventsController : Controller
    {
        private readonly ICallAutomationService callAutomationService;
        private readonly IChatService chatService;
        private readonly ILogger logger;
        private readonly EventConverter eventConverter;

        public EventsController(
            ICallAutomationService callAutomationService,
            IChatService chatService,
            ILogger<EventsController> logger)
        {
            this.callAutomationService = callAutomationService;
            this.chatService = chatService;
            this.logger = logger;
            eventConverter = new EventConverter();
        }

        /* Route for Azure Communication Service eventgrid webhooks */
        [HttpPost]
        [Route("events")]
        public async Task<IActionResult> Handle([FromBody] EventGridEvent[] eventGridEvents, [FromQuery(Name = "callConnectionId")] string? callConnectionId)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                logger.LogInformation("(DEBUG) Received ACS event: {event}", eventGridEvent.EventType);

                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };

                        return Ok(responseData);
                    }
                }

                var data = eventConverter.Convert(eventGridEvent);

                switch (data)
                {
                    case null:
                        continue;

                    case AcsIncomingCallEventData incomingCall:
                        await callAutomationService.HandleEvent(incomingCall);
                        break;

                    case AcsChatMessageReceivedInThreadEventData chatMessageReceived:
                        //await chatService.HandleEvent(chatMessageReceived);
                        break;

                    case AcsRecordingFileStatusUpdatedEventData recordingFileStatusUpdated:
                        await callAutomationService.HandleEvent(recordingFileStatusUpdated, callConnectionId!);
                        break;

                    case CallEndedEvent callEnded:
                        await callAutomationService.HandleEvent(callEnded);
                        break;
                }
            }

            return Ok();
        }

        /* Route for CallAutomation in-call event callbacks */
        [HttpPost]
        [Route("callbacks")]
        public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents)
        {
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);

                logger.LogInformation("(DEBUG) Received Call Automation event: {type}", parsedEvent.GetType());                

                switch (parsedEvent)
                {
                    case CallConnected callConnected:
                        await callAutomationService.HandleEvent(callConnected);
                        break;

                    case RecognizeCompleted recognizeCompleted:
                        await callAutomationService.HandleEvent(recognizeCompleted);
                        break;

                    case RecognizeFailed recognizeFailed:
                        await callAutomationService.HandleEvent(recognizeFailed);
                        break;

                    case PlayCompleted playCompleted:
                        await callAutomationService.HandleEvent(playCompleted);
                        break;

                    case PlayFailed playFailed:
                        await callAutomationService.HandleEvent(playFailed);
                        break;

                    case CallDisconnected callDisconnected:
                        await callAutomationService.HandleEvent(callDisconnected);
                        break;

                    default:
                        break;
                }
            }

            return Ok();
        }
    }
}