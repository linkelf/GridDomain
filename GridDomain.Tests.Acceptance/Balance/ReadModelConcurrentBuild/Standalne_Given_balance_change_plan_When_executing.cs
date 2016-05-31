using GridDomain.Balance.Node;
using GridDomain.Node;
using GridDomain.Node.Configuration;

namespace GridDomain.Tests.Acceptance.Balance.ReadModelConcurrentBuild
{
    public class Standalne_Given_balance_change_plan_When_executing: Given_balance_change_plan_When_executing
    {

        protected override GridDomainNode GreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig)
        {
            return new GridDomainNode(DefaultUnityContainer(dbConfig),
                                      new BalanceCommandsRouting(), 
                                      ActorSystemFactory.CreateActorSystem(akkaConf),
                                      TransportMode.Standalone);
        }

    }
}