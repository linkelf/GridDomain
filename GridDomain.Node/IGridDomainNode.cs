using System;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.CQRS;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Transport;
using Serilog;

namespace GridDomain.Node
{
    public interface IGridDomainNode : ICommandExecutor,
                                       IMessageWaiterFactory,
                                       IDisposable {}
    
    public interface IExtendedGridDomainNode : IGridDomainNode
    {
        ActorSystem System { get; }
        TimeSpan DefaultTimeout { get; }
        IActorTransport Transport { get; }
        IActorCommandPipe Pipe { get; }
        Task Start();
        Task Stop();
        ILogger Log { get; }
        EventsAdaptersCatalog EventsAdaptersCatalog { get; }
    }

}