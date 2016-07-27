using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Sagas.StateSagas.SampleSaga
{
    public class PersonRememberedEvent : DomainEvent
    {
        public Guid PersonId { get; }

        public PersonRememberedEvent(Guid sourceId,Guid personId):base(sourceId)
        {
            PersonId = personId;
        }
    }
}