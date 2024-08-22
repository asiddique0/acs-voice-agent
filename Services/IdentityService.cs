namespace LumenicBackend.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly CommunicationIdentityClient client;

        public IdentityService(IConfiguration configuration)
        {
            var acsConnectionString = configuration["AcsConnectionString"] ?? string.Empty;
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
            client = new CommunicationIdentityClient(acsConnectionString);
        }

        public string GetNewUserId()
        {
            var identityResponse = client.CreateUser();
            return identityResponse.Value.ToString();
        }

        public async Task<(string, string)> GetNewUserIdAndToken()
        {
            var identityResponse = await client.CreateUserAndTokenAsync(
                scopes: [CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP ]);
            return (identityResponse.Value.User.Id, identityResponse.Value.AccessToken.Token);
        }

        public async Task<string> GetTokenForUserId(string userId)
        {
            var identityResponse = await client.GetTokenAsync(
                new CommunicationUserIdentifier(userId),
                scopes: [ CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP ]);
            return identityResponse.Value.Token;
        }

        public async Task DeleteUserByUserId(string userId)
        {
            await client.DeleteUserAsync(new CommunicationUserIdentifier(userId));
        }

        public async Task RevokeTokensByUserId (string userId)
        {
            await client.RevokeTokensAsync(new CommunicationUserIdentifier(userId));
        }
    }
}