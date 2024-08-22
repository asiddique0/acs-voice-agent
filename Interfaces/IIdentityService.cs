namespace LumenicBackend.Interfaces
{
    public interface IIdentityService
    {
        string GetNewUserId();

        Task<(string, string)> GetNewUserIdAndToken();

        Task<string> GetTokenForUserId(string userId);

        Task DeleteUserByUserId(string userId);

        Task RevokeTokensByUserId(string userId);
    }
}