using System;
using GridDomain.EventSourcing;

namespace GridDomain.Scheduling.FutureEvents
{
    public class FutureEventCanceledEvent : DomainEvent
    {
        public FutureEventCanceledEvent(Guid futureEventId,
                                        Guid sourceId,
                                        string sourceName,
                                        DateTime? createdTime = null,
                                        Guid? sagaId = null) : base(sourceId,  sagaId, createdTime: createdTime)
        {
            FutureEventId = futureEventId;
            SourceName = sourceName;
        }

        public Guid FutureEventId { get; }
        public string SourceName { get; }
    }
}