using System;
using System.Collections.Generic;
using System.Linq;

namespace GridDomain.CQRS.Messaging.MessageRouting
{
    public class ProjectionGroup: IProjectionGroup
    {
        private readonly IServiceLocator _locator;
        readonly Dictionary<Type, List<Action<object>>> _handlers = new Dictionary<Type, List<Action<object>>>();

        public ProjectionGroup(IServiceLocator locator)
        {
            _locator = locator;
        }

        public void Add<TMessage, THandler>(string correlationPropertyName ) where THandler : IHandler<TMessage>
        {
            var handler = _locator.Resolve<THandler>();

            List<Action<object>> builderList;

            if (!_handlers.TryGetValue(typeof (TMessage), out builderList))
            {
                builderList = new List<Action<object>>();
                _handlers[typeof (TMessage)] = builderList;
            }
            builderList.Add(o => handler.Handle((TMessage) o));

            if(_acceptMessages.All(m => m.MessageType != typeof (TMessage)))
                _acceptMessages.Add(new MessageRoute(typeof(TMessage), correlationPropertyName));
        }

        public void Project(object message)
        {
            var msgType = message.GetType();
            foreach(var handler in _handlers[msgType])
                handler(message);
        }
        private readonly List<MessageRoute> _acceptMessages = new List<MessageRoute>();
        public IReadOnlyCollection<MessageRoute> AcceptMessages => _acceptMessages;
    }
}