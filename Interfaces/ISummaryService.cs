using LumenicBackend.Models.Requests;

namespace LumenicBackend.Interfaces
{
    public interface ISummaryService
    {
        Task<string> SendSummaryEmail(SummaryRequest summary);
    }
}