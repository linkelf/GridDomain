using System;
using System.Linq.Expressions;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;

namespace GridDomain.Node.Configuration.Composition {
    class MessageHandlerFactory<TMessage, THandler> where THandler : IHandler<TMessage>
                                                    where TMessage : class, IHaveSagaId, IHaveId
    {
        private readonly Func<IMessageProcessContext, THandler> _creator;
        private readonly Expression<Func<TMessage, Guid>> _correlationPropertyExpression;

        protected MessageHandlerFactory(Func<IMessageProcessContext, THandler> creator, Expression<Func<TMessage, Guid>> correlationPropertyExpression)
        {
            _creator = creator;
            _correlationPropertyExpression = correlationPropertyExpression;
        }

        public THandler Create(IMessageProcessContext context)
        {
            return _creator(context);
        }

        public IMessageRouteMap CreateRouteMap()
        {
            return MessageRouteMap.New<TMessage, THandler>(_correlationPropertyExpression,$"autogenerated map for {typeof(TMessage).Name} to {typeof(THandler).Name}");
        }
    }
}