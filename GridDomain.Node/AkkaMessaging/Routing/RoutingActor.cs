using System;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using NLog;

namespace GridDomain.Node.AkkaMessaging.Routing
{
    public abstract class RoutingActor : TypedActor, IHandler<CreateHandlerRouteMessage>,
                                                     IHandler<CreateActorRouteMessage>
    {
        private readonly IHandlerActorTypeFactory _actorTypeFactory;
        protected readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly IActorSubscriber _subscriber;

        protected RoutingActor(IHandlerActorTypeFactory actorTypeFactory,
            IActorSubscriber subscriber)
        {
            _subscriber = subscriber;
            _actorTypeFactory = actorTypeFactory;
        }

        public void Handle(CreateActorRouteMessage msg)
        {
            var handleActor = CreateActor(msg.ActorType, CreateActorRouter(msg), msg.ActorName);
            foreach (var msgRoute in msg.Routes)
                _subscriber.Subscribe(msgRoute.MessageType, handleActor, Self);
        }

        public void Handle(CreateHandlerRouteMessage msg)
        {
            var actorType = _actorTypeFactory.GetActorTypeFor(msg.MessageType, msg.HandlerType);
            string actorName = $"{msg.HandlerType}_for_{msg.MessageType.Name}";
            Self.Tell(new CreateActorRouteMessage(actorType,actorName,new MessageRoute(msg.MessageType,msg.MessageCorrelationProperty)));
        }

        protected virtual Pool CreateActorRouter(CreateActorRouteMessage msg)
        {
            var routesMap = msg.Routes.ToDictionary(r => r.Topic, r => r.CorrelationField);

            var pool =
                new ConsistentHashingPool(Environment.ProcessorCount)
                    .WithHashMapping(m =>
                    {
                        var type = m.GetType();
                        string prop = null;

                        if(routesMap.TryGetValue(type.FullName,out prop))
                            return type.GetProperty(prop).GetValue(m);

                        if (typeof(ICommandFault).IsAssignableFrom(type))
                        {
                            prop = routesMap[typeof(ICommandFault).FullName];
                            return typeof(ICommandFault).GetProperty(prop).GetValue(m);
                        }

                        throw new ArgumentException();
                    });

            return pool;
        }

        private IActorRef CreateActor(Type actorType, 
                                      RouterConfig routeConfig,
                                      string actorName)
        {
            var handleActorProps = Context.System.DI().Props(actorType);
            handleActorProps = handleActorProps.WithRouter(routeConfig);

            var handleActor = Context.System.ActorOf(handleActorProps, actorName);
            return handleActor;
        }
    }
}