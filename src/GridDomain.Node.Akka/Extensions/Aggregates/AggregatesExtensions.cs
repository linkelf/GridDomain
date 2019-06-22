using System;
using Akka.Actor;
using Autofac;

namespace GridDomain.Node.Akka.Extensions.Aggregates {
    public static class AggregatesExtensions
    {

        public static AggregatesExtension GetAggregatesExtension(this ActorSystem sys)
        {
            return sys.GetExtension<AggregatesExtension>();
        }

            
        public static AggregatesExtension InitAggregatesExtension(this ActorSystem system, ContainerBuilder builder=null)
        {
            if(system == null)
                throw new ArgumentNullException(nameof(system));

            return (AggregatesExtension)system.RegisterExtension(new AggregatesExtensionProvider(builder));
        }
    }
}