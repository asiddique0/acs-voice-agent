using LumenicBackend.Models.Requests;

namespace LumenicBackend.Interfaces
{
    public interface INumberService
    {
        public Task<PhoneNumberSearchResult> SearchForPhoneNumber(string areaCode);
        public Task<Response> PurchasePhoneNumber(PurchaseRequest purchaseRequest);
        public List<Number> GetNumbers(string organizationId);
        public Task<List<Number>> GetAllNumbers();
        public Task<List<PurchasedPhoneNumber>> GetAllPurchasedNumbers();
        public Task<Response> ReleasePhoneNumber(string phoneNumber);
    }
}
