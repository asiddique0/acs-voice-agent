namespace LumenicBackend.Services
{
    public class NumberService : INumberService
    {
        private readonly PhoneNumbersClient client;
        private readonly DatabaseService databaseService;
        private readonly ILogger logger;

        public NumberService(IConfiguration configuration, DatabaseService databaseService, ILogger<NumberService> logger)
        {           
            var acsConnectionString = configuration["AcsConnectionString"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString, nameof(acsConnectionString));

            this.logger = logger;
            this.client = new PhoneNumbersClient(acsConnectionString);
            this.databaseService = databaseService;
        }

        public async Task<PhoneNumberSearchResult> SearchForPhoneNumber(string areaCode)
        {
            var phoneNumberOptions = new PhoneNumberSearchOptions()
            {
                AreaCode = areaCode,
                Quantity = 1,
            };
            var phoneNumberCapabilities = new PhoneNumberCapabilities(calling: PhoneNumberCapabilityType.InboundOutbound, sms: PhoneNumberCapabilityType.InboundOutbound);

            var searchOperation = await client.StartSearchAvailablePhoneNumbersAsync("US", PhoneNumberType.TollFree, PhoneNumberAssignmentType.Application, phoneNumberCapabilities, phoneNumberOptions);
            return await searchOperation.WaitForCompletionAsync();

        }

        public async Task<Response> PurchasePhoneNumber(PurchaseRequest purchaseRequest)
        {
            var purchaseOperation = await client.StartPurchasePhoneNumbersAsync(purchaseRequest.SearchId);
            var purchaseResult = await purchaseOperation.WaitForCompletionResponseAsync();

            if (!purchaseResult.IsError)
            {
                var searchResult = await client.GetPhoneNumberSearchResultAsync(purchaseRequest.SearchId);
                var number = new Number()
                {
                    Id = Guid.NewGuid(),
                    NumberValue = searchResult.Value.PhoneNumbers.ElementAt(0),
                    TransferNumber = purchaseRequest.TransferNumber,
                    TransferWeight = purchaseRequest.TransferWeight,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    StartHour = purchaseRequest.StartHour,
                    EndHour = purchaseRequest.EndHour,
                    TimeZone = purchaseRequest.TimeZone,
                    OrganizationId = Guid.Parse(purchaseRequest.OrganizationId)
                };
                await databaseService.AddNumber(number);
            }

            return purchaseResult;
        }

        public List<Number> GetNumbers(string organizationId)
        {
            return databaseService.GetNumbersByOrganizationId(organizationId);
        }

        public async Task<List<PurchasedPhoneNumber>> GetAllPurchasedNumbers()
        {
            var purchasedPhoneNumbers = new List<PurchasedPhoneNumber>();

            var purchasedPhoneNumbersAsyncPages = client.GetPurchasedPhoneNumbersAsync();

            await foreach (var phoneEntry in purchasedPhoneNumbersAsyncPages)
            {
                purchasedPhoneNumbers.Add(phoneEntry);
            }

            return purchasedPhoneNumbers;
        }

        public async Task<List<Number>> GetAllNumbers()
        {
            return await databaseService.GetAllNumbers();
        }

        public async Task<Response> ReleasePhoneNumber(string phoneNumber)
        {
            var releaseOperation = await client.StartReleasePhoneNumberAsync(phoneNumber);
            var resultResult = await releaseOperation.WaitForCompletionResponseAsync();
            
            if (!resultResult.IsError)
            {
                await databaseService.DeleteNumber(phoneNumber);
            }

            return resultResult;
        }
    }
}
