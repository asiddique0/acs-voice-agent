namespace LumenicBackend.Interfaces
{
    public interface ITranscriptionService
    {
        Task TranscribeVoiceMessageToChat(string userId, string userToken, string threadId, string text, string displayName);
    }
}