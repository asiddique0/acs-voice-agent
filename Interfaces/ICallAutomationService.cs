namespace LumenicBackend.Interfaces
{
    public interface ICallAutomationService
    {
        Task<CreateCallResult> CreateCallAsync(string botNumber, string userNumber);

        Task HandleEvent(CallConnected callConnected);

        Task HandleEvent(RecognizeCompleted recognizeCompleted);

        Task HandleEvent(RecognizeFailed recognizeFailedEvent);

        Task HandleEvent(PlayCompleted playCompletedEvent);

        Task HandleEvent(PlayFailed playFailedEvent);

        Task HandleEvent(AcsIncomingCallEventData incomingCallEvent);
 
        Task HandleEvent(AcsRecordingFileStatusUpdatedEventData recordingEventData, string callConnectionId);

        Task HandleEvent(CallDisconnected callDisconnectedEvent);

        Task HandleEvent(CallEndedEvent callEndedEvent);
    }
}
