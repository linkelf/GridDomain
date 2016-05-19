using System;
using GridDomain.EventSourcing;

namespace GridDomain.CQRS.Messaging.MessageRouting.Sagas
{
    public class SagaTransitionEvent<TState,TTransition> : DomainEvent
    {
        public SagaTransitionEvent(TTransition transition, TState state, Guid sourceId, DateTime? createdTime = null) : base(sourceId, createdTime)
        {
            Transition = transition;
            State = state;
        }

        public TTransition Transition { get; }
        public TState State { get; }
    }
}