﻿using System;
using System.CodeDom;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Unity;
using GridDomain.Balance.Node;
using GridDomain.Node;
using GridDomain.Node.Configuration;
using GridDomain.Tests.Acceptance.Persistence;
using NUnit.Framework;

namespace GridDomain.Tests.Acceptance.Balance.ReadModelConcurrentBuild
{
    public class Cluster_Given_balance_change_plan_When_executing: Given_balance_change_plan_When_executing
    {
        private AkkaCluster _akkaCluster;

        protected override GridDomainNode GreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig)
        {
            _akkaCluster = ActorSystemFactory.CreateCluster(akkaConf);
            var container = DefaultUnityContainer(dbConfig);

            return new GridDomainNode(container, 
                                      new BalanceCommandsRouting(),
                                      TransportMode.Cluster, _akkaCluster.All);
        }

        /// <summary>
        /// Important than persistence setting are the same as for testing cluster as for test ActorSystem
        /// </summary>
        public Cluster_Given_balance_change_plan_When_executing() : base(AkkaConf.Copy("writeModelCheckSystem",9000)
                                                                                 .ToStandAloneSystemConfig())
        {
          
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            _akkaCluster.Dispose();
        }

        protected override int BusinessNum => 5;
        protected override int ChangesPerBusiness => 10;
    }
}