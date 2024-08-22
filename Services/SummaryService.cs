namespace LumenicBackend.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly IChatService chatService;
        private readonly IArtificialIntelligenceProvider deepInfraService;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        private readonly string acsConnectionString;

        public SummaryService(
            IChatService chatService,
            [FromKeyedServices("DeepInfraService")] IArtificialIntelligenceProvider deepInfraService,
            IConfiguration configuration,
            ILogger<SummaryService> logger)
        {
            this.chatService = chatService;
            this.deepInfraService = deepInfraService;
            this.logger = logger;
            this.configuration = configuration;
            acsConnectionString = this.configuration["AcsConnectionString"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
        }

        public async Task<string> SendSummaryEmail(SummaryRequest summary)
        {
            var htmlContent = summary.Body;
            var sender = summary.Sender;
            var recipient = summary.Recipient;
            try
            {
                logger.LogInformation("Sending email: to={}, from={}, body={}", recipient, sender, htmlContent);
                ArgumentException.ThrowIfNullOrEmpty(sender);
                ArgumentException.ThrowIfNullOrEmpty(recipient);
                // Note: 
                // This quickstart sample uses receiver email address from app configuration for simplicity
                // In production scenario customer would provide their preferred email address
                EmailClient emailClient = new(this.acsConnectionString);
                EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                    WaitUntil.Completed,
                    sender,
                    recipient,
                    "Follow up on support conversation",
                    htmlContent);
                return emailSendOperation.Value.Status.ToString();
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
                return ex.ErrorCode ?? "EmailSendFailed";
            }
        }
    }
}