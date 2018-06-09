using Akka.Actor;
using GridDomain.Node.Configuration.Hocon;

namespace GridDomain.Node.Configuration
{
    public static class NodeConfigurationExtensions
    {
        public static ActorSystem CreateInMemorySystem(this NodeConfiguration conf)
        {
            return ActorSystem.Create(conf.Name, conf.ToStandAloneInMemorySystem().Build());
        }

        public static ActorSystemConfigBuilder ToStandAloneInMemorySystem(this NodeConfiguration conf, bool serializeMessagesCreators = false)
        {
            return conf.ConfigureStandAloneInMemorySystem(ActorSystemConfigBuilder.New(), serializeMessagesCreators);
        }

        public static ActorSystemConfigBuilder ConfigureStandAloneInMemorySystem(this NodeConfiguration conf, ActorSystemConfigBuilder configBuilder, bool serializeMessagesCreators = false)
        {
            return configBuilder.LocalInMemory(serializeMessagesCreators)
                          .Log(conf.LogLevel)
                          .Remote(conf.Address);
        }
    }
}