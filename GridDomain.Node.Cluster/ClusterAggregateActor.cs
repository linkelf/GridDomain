﻿using Akka.Actor;
using Akka.Cluster.Sharding;
using GridDomain.Configuration;
using GridDomain.Configuration.SnapshotPolicies;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.EventSourced.Messages;

namespace GridDomain.Node.Cluster
{
    public class ClusterAggregateActor<T> : AggregateActor<T> where T : class, IAggregate
    {
        public ClusterAggregateActor(IAggregateCommandsHandler<T> handler,
                                     ISnapshotsPersistencePolicy snapshotsPersistencePolicy,
                                     IConstructAggregates aggregateConstructor,
                                     IConstructSnapshots snapshotsConstructor,
                                     IActorRef customHandlersActor,
                                     IRecycleConfiguration recycle
                                     ) : base(handler,
                                                                           snapshotsPersistencePolicy,
                                                                           aggregateConstructor,
                                                                           snapshotsConstructor,
                                                                           customHandlersActor)
        {
            Context.SetReceiveTimeout(recycle.ChildMaxInactiveTime);
            
        }

        protected override void AwaitingCommandBehavior()
        {
            Command<ReceiveTimeout>(_ =>
                                    {
                                        
                                        Log.Debug("Going to passivate");
                                        Context.Parent.Tell(new Passivate(Shutdown.Request.Instance));
                                    });
            base.AwaitingCommandBehavior();
        }
    }
}