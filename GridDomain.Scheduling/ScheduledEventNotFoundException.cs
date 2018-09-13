using System;

namespace GridDomain.Scheduling
{
    public class ScheduledEventNotFoundException : Exception
    {
    
        public ScheduledEventNotFoundException(string eventId)
        {
            EventId = eventId;
        }

        public string EventId { get; }
    }
}