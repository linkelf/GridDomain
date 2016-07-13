using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    public abstract class MessageWaiter<T> : UntypedActor where T : ExpectedMessage
    {
        private readonly Dictionary<Type, int> MessageCounters;
        private readonly Dictionary<Type, T> MessageWaits;
        private readonly IActorRef _notifyActor;
        private readonly List<object> _allReceivedEvents;

        protected MessageWaiter(IActorRef notifyActor, params T[] expectedMessages)
        {
            _notifyActor = notifyActor;
            MessageCounters = expectedMessages.ToDictionary(m => m.MessageType, m => m.MessageCount);
            MessageWaits = expectedMessages.ToDictionary(m => m.MessageType, m => m);

            _allReceivedEvents = new List<object>();
        }

        /// <summary>
        /// Will count only messages of known type and with known Id, if IdPropertyName is specified
        /// </summary>
        /// <param name="message"></param>
        protected override void OnReceive(object message)
        {
            var type = message.GetType();
            _allReceivedEvents.Add(message);

            if (!MessageCounters.ContainsKey(type)) return;

            var wait = MessageWaits[type];
            var waitsForEventWithId = !string.IsNullOrEmpty(wait.IdPropertyName);

            if (waitsForEventWithId)
            {
                var messageId = type.GetProperty(wait.IdPropertyName).GetValue(message);
                if (wait.MessageId != (Guid)messageId) return;
            }

            --MessageCounters[type];
            if (CanContinue(MessageCounters)) return;

            _notifyActor.Tell(BuildAnswerMessage(message));
        }

        protected override bool AroundReceive(Receive receive, object message)
        {
            return base.AroundReceive(receive, message);
        }

        protected abstract bool CanContinue(Dictionary<Type, int> messageCounters);
        protected virtual object BuildAnswerMessage(object message)
        {
            return new ExpectedMessagesRecieved(message, _allReceivedEvents);
        }
    }

    
    public class AllMessageWaiter : MessageWaiter<ExpectedMessage>
    {
        protected override bool CanContinue(Dictionary<Type, int> messageCounters)
        {
            return messageCounters.Any(c => c.Value > 0);
        }

        public AllMessageWaiter(IActorRef notifyActor, params ExpectedMessage[] expectedMessages) : base(notifyActor, expectedMessages)
        {
        }
    }

    public class AnyMessageWaiter : MessageWaiter<ExpectedMessage>
    {
        protected override bool CanContinue(Dictionary<Type, int> messageCounters)
        {
            return messageCounters.All(c => c.Value > 0);
        }

        public AnyMessageWaiter(IActorRef notifyActor, params ExpectedMessage[] expectedMessages) : base(notifyActor, expectedMessages)
        {
        }
    }
}