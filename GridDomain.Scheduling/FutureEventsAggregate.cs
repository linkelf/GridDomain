﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;

namespace GridDomain.Scheduling
{
    public class FutureEventsAggregate : ConventionAggregate
    {
        protected FutureEventsAggregate(Guid id):base(id)
        {
            _schedulingSourceName = GetType().Name;
        }

        public IEnumerable<FutureEventScheduledEvent> FutureEvents  =>_futureEvents;
        readonly List<FutureEventScheduledEvent> _futureEvents = new List<FutureEventScheduledEvent>();
        private readonly string _schedulingSourceName;

      
        public async Task RaiseScheduledEvent(Guid futureEventId, Guid futureEventOccuredEventId)
        {
            FutureEventScheduledEvent ev = FutureEvents.FirstOrDefault(e => e.Id == futureEventId);
            if (ev == null)
                throw new ScheduledEventNotFoundException(futureEventId);

            var futureEventOccuredEvent = new FutureEventOccuredEvent(futureEventOccuredEventId, futureEventId, Id);

            await Emit(ev.Event);
            //wait for event apply in case of errors; 
            Produce(futureEventOccuredEvent);
        }

        protected void Produce(DomainEvent @event, DateTime raiseTime, Guid? futureEventId = null)
        {
             Produce(new FutureEventScheduledEvent(futureEventId ?? Guid.NewGuid(), Id, raiseTime, @event, _schedulingSourceName));
        }
        protected Task Emit(DomainEvent @event, DateTime raiseTime, Guid? futureEventId = null)
        {
            return Emit(new FutureEventScheduledEvent(futureEventId ?? Guid.NewGuid(), Id, raiseTime, @event, _schedulingSourceName));
        }

        protected void CancelScheduledEvents<TEvent>(Predicate<TEvent> criteia = null) where TEvent : DomainEvent
        {
            var eventsToCancel = FutureEvents.Where(fe => fe.Event is TEvent);
            if (criteia != null)
                eventsToCancel = eventsToCancel.Where(e => criteia((TEvent) e.Event));

            var domainEvents = eventsToCancel.Select(e => new FutureEventCanceledEvent(e.Id, Id, _schedulingSourceName))
                                             .Cast<DomainEvent>()
                                             .ToArray();
            Produce(domainEvents);
        }

        protected void Apply(FutureEventScheduledEvent e)
        {
            _futureEvents.Add(e);
        }

        protected void Apply(FutureEventOccuredEvent e)
        {
            DeleteFutureEvent(e.FutureEventId);
        }

        protected void Apply(FutureEventCanceledEvent e)
        {
            DeleteFutureEvent(e.FutureEventId);
        }

        private void DeleteFutureEvent(Guid futureEventId)
        {
            FutureEventScheduledEvent evt = FutureEvents.FirstOrDefault(e => e.Id == futureEventId);
            if (evt == null)
                throw new ScheduledEventNotFoundException(futureEventId);
            _futureEvents.Remove(evt);
        }
    }
}