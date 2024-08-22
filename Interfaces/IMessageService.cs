namespace LumenicBackend.Interfaces
{
    public interface IMessageService
    {
        Task<SmsSendResult> SendTextMessage(string senderPhoneNumber, string targetPhoneNumber, string message);
    }
}