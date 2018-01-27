using System;
using GridDomain.Configuration;
using GridDomain.Node.Actors.ProcessManagers;
using GridDomain.Node.Actors.ProcessManagers.Messages;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.State;

namespace GridDomain.Node.Actors.Aggregates {
    public class ProcessStateHubActor<TState> : AggregateHubActor<ProcessStateAggregate<TState>> where TState : IProcessState
    {
        public ProcessStateHubActor(IPersistentChildsRecycleConfiguration conf) : base(conf)
        {
            ChildActorType = typeof(ProcessStateActor<TState>);
            Receive<GetProcessState>(s =>
                                     {
                                         SendToChild(s, s.Id, GetChildActorName(s.Id), Sender);
                                     });
        }
        protected override Type ChildActorType { get; }

        
    }
}