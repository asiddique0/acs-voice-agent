﻿namespace LumenicBackend.Events
{
    /* 
    * Helper class used to handle communication event models that are in preview, 
    * and not yet part of EventGrid.SystemEvents SDK 
    */
    public sealed class EventConverter
    {
        internal const string CallEndedEventName = "CallEnded";
        internal const string ChatMessageReceivedInThreadName = "ChatMessageReceivedInThread";
        internal const string IncomingCallName = "IncomingCall";
        internal const string RecordingFileStatusUpdated = "RecordingFileStatusUpdated";

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public object? Convert(EventGridEvent eventGridEvent)
        {
            var data = eventGridEvent.Data;
            if (data is null) throw new ArgumentNullException($"No data present: {eventGridEvent}");

            return ParseEventType(eventGridEvent.EventType) switch
            {
                ChatMessageReceivedInThreadName => data.ToObjectFromJson<AcsChatMessageReceivedInThreadEventData>(JsonOptions),
                CallEndedEventName => data.ToObjectFromJson<CallEndedEvent>(JsonOptions),
                IncomingCallName => data.ToObjectFromJson<AcsIncomingCallEventData>(JsonOptions),
                RecordingFileStatusUpdated => data.ToObjectFromJson<AcsRecordingFileStatusUpdatedEventData>(JsonOptions),
                _ => null
            };
        }

        private static string ParseEventType(string eventType)
        {
            var split = eventType.Split("Microsoft.Communication.");
            return split[^1];
        }
    }
}
