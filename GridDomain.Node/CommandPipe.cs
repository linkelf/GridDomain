using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using Akka.Routing;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.CommandPipe.Processors;
using GridDomain.Node.Actors.Hadlers;
using GridDomain.Node.Actors.Sagas;
using Microsoft.Practices.Unity;

namespace GridDomain.Node
{
    public class CommandPipe : IMessagesRouter
    {
        private readonly TypeCatalog<IMessageProcessor, ICommand> _aggregatesCatalog = new TypeCatalog<IMessageProcessor, ICommand>();
        private readonly IUnityContainer _container;
        private readonly ProcessorListCatalog _handlersCatalog = new ProcessorListCatalog();

        private readonly ILoggingAdapter _log;
        private readonly ProcessorListCatalog<ISagaTransitCompleted> _sagaCatalog = new ProcessorListCatalog<ISagaTransitCompleted>();
        private readonly ActorSystem _system;

        public CommandPipe(ActorSystem system, IUnityContainer container)
        {
            _container = container;
            _system = system;
            _log = system.Log;
        }

        public IActorRef SagaProcessor { get; private set; }
        public IActorRef HandlersProcessor { get; private set; }
        public IActorRef CommandExecutor { get; private set; }

        public Task RegisterAggregate(IAggregateCommandsHandlerDescriptor descriptor)
        {
            var aggregateHubType = typeof(AggregateHubActor<>).MakeGenericType(descriptor.AggregateType);

            var aggregateActor = CreateActor(aggregateHubType, descriptor.AggregateType.BeautyName() + "_Hub");

            var processor = new FireAndForgetMessageProcessor(aggregateActor);

            foreach (var aggregateCommandInfo in descriptor.RegisteredCommands)
                _aggregatesCatalog.Add(aggregateCommandInfo, processor);

            return Task.CompletedTask;
        }

        public Task RegisterSaga(ISagaDescriptor sagaDescriptor, string name = null)
        {
            var sagaActorType = typeof(SagaHubActor<>).MakeGenericType(sagaDescriptor.StateType);

            var sagaActor = CreateActor(sagaActorType, name ?? sagaDescriptor.StateMachineType.BeautyName() + "_Hub");
            var processor = new SynchroniousMessageProcessor<ISagaTransitCompleted>(sagaActor);

            foreach (var acceptMsg in sagaDescriptor.AcceptMessages)
                _sagaCatalog.Add(acceptMsg.MessageType, processor);

            return Task.CompletedTask;
        }

        public Task RegisterSyncHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                                                 where TMessage : class, IHaveSagaId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new SynchroniousMessageProcessor<HandlerExecuted>(actor));
        }
        public Task RegisterFireAndForgetHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                              where TMessage : class, IHaveSagaId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new FireAndForgetMessageProcessor(actor));
        }
        public Task RegisterParralelHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                                       where TMessage : class, IHaveSagaId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new ParrallelMessageProcessor<HandlerExecuted>(actor));
        }

        /// <summary>
        /// </summary>
        /// <returns>Reference to pipe actor for command execution</returns>
        public async Task<IActorRef> Init()
        {
            _log.Debug("Command pipe is starting");

            SagaProcessor = _system.ActorOf(Props.Create(() => new SagaPipeActor(_sagaCatalog)), nameof(SagaPipeActor));

            HandlersProcessor = _system.ActorOf(
                                                Props.Create(() => new HandlersPipeActor(_handlersCatalog, SagaProcessor)),
                                                nameof(HandlersPipeActor));

            CommandExecutor = _system.ActorOf(Props.Create(() => new AggregatesPipeActor(_aggregatesCatalog)),
                                              nameof(AggregatesPipeActor));

            _container.RegisterInstance(HandlersPipeActor.CustomHandlersProcessActorRegistrationName, HandlersProcessor);
            _container.RegisterInstance(SagaPipeActor.SagaProcessActorRegistrationName, SagaProcessor);

            await SagaProcessor.Ask<Initialized>(new Initialize(CommandExecutor));
            return CommandExecutor;
        }

        private Task RegisterHandler<TMessage, THandler>(Func<IActorRef, IMessageProcessor> processorCreator) where THandler : IHandler<TMessage>
                                                                           where TMessage : class, IHaveSagaId, IHaveId
        {
            var handlerActorType = typeof(MessageProcessActor<TMessage, THandler>);
            var handlerActor = CreateActor(handlerActorType, handlerActorType.BeautyName());

            _handlersCatalog.Add<TMessage>(processorCreator(handlerActor));
            return Task.CompletedTask;
        }

        private IActorRef CreateActor(Type actorType, string actorName, RouterConfig routeConfig = null)
        {
            var actorProps = _system.DI().Props(actorType);
            if (routeConfig != null)
                actorProps = actorProps.WithRouter(routeConfig);

            var actorRef = _system.ActorOf(actorProps, actorName);
            return actorRef;
        }
    }
}