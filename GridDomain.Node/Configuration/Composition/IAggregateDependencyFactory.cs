using System;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors;

namespace GridDomain.Node.Configuration.Composition
{
    public interface IAggregateDependencyFactory<TAggregate> where TAggregate : Aggregate
    {
        IAggregateCommandsHandler<TAggregate> CreateCommandsHandler();
        ISnapshotsPersistencePolicy CreatePersistencePolicy();
        IConstructAggregates CreateFactory();
        IPersistentChildsRecycleConfiguration CreateRecycleConfiguration();
    }

    public static class DefaultAggregateDependencyFactory
    {
        public static DefaultAggregateDependencyFactory<TAggregate> New<TAggregate>(IAggregateCommandsHandler<TAggregate> handler) where TAggregate : Aggregate
        {
            return new DefaultAggregateDependencyFactory<TAggregate>(() => handler);
        }
    }
    public class DefaultAggregateDependencyFactory<TAggregate> : IAggregateDependencyFactory<TAggregate> where TAggregate : Aggregate
    {
        public Func<IAggregateCommandsHandler<TAggregate>> HandlerCreator { protected get; set; }
        public Func<ISnapshotsPersistencePolicy> SnapshotPolicyCreator { protected get; set; }
        public Func<IConstructAggregates> AggregateFactoryCreator { protected get; set; }
        public Func<IPersistentChildsRecycleConfiguration> RecycleConfigurationCreator { protected get; set; }

        public DefaultAggregateDependencyFactory(Func<IAggregateCommandsHandler<TAggregate>> handler)
        {
            HandlerCreator = handler;
            SnapshotPolicyCreator = () => new NoSnapshotsPersistencePolicy();
            AggregateFactoryCreator = () => new AggregateFactory();
            RecycleConfigurationCreator = () => new DefaultPersistentChildsRecycleConfiguration();
        }

        public virtual IAggregateCommandsHandler<TAggregate> CreateCommandsHandler()
        {
            return HandlerCreator == null ? throw new CannotCreateCommandHandlerExeption() : HandlerCreator();
        }

        public virtual ISnapshotsPersistencePolicy CreatePersistencePolicy()
        {
            return SnapshotPolicyCreator();
        }

        public virtual IConstructAggregates CreateFactory()
        {
            return AggregateFactoryCreator();
        }

        public virtual IPersistentChildsRecycleConfiguration CreateRecycleConfiguration()
        {
            return RecycleConfigurationCreator();
        }
    }

    public class CannotCreateCommandHandlerExeption : Exception { }
}