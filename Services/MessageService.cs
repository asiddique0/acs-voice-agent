namespace LumenicBackend.Services
{
    public class MessageService : IMessageService
    {
        private readonly ILogger logger;
        private readonly SmsClient smsClient;
        private readonly string callJoinUrl;

        public MessageService(
            IConfiguration configuration,
            ILogger<MessageService> logger)
        {
            this.logger = logger;
            string acsConnectionString = configuration["AcsConnectionString"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);

            this.smsClient = new SmsClient(acsConnectionString);

            // Note: As this sample supports only one conversation at a time
            // there is no need to embed call identifier to url. So the URL is static
            string hostUrl = configuration["HostUrl"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(hostUrl);

            this.callJoinUrl = $"{hostUrl}?callerType=Customer";
        }

        public async Task<SmsSendResult> SendTextMessage(string senderPhoneNumber, string targetPhoneNumber, string message)
        {
            SmsSendResult resp = await smsClient.SendAsync(
                from: senderPhoneNumber, // Your E.164 formatted from phone number used to send SMS
                to: targetPhoneNumber, // E.164 formatted recipient phone number
                message: message);
            logger.LogInformation("Sent SMS message, to={target}, message={message}", targetPhoneNumber, message);
            return resp;
        }
    }
}