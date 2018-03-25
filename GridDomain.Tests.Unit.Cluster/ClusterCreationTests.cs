﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Event;
using Akka.Streams.Implementation.Fusing;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using GridDomain.Common;
using GridDomain.Node.Cluster;
using GridDomain.Node.Configuration;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.Cluster
{
    /// <summary>
    /// GIVEN actor system builder
    /// WHEN building a cluster
    /// </summary>
    public class ClusterConfigurationTests : IDisposable
    {
        private ClusterInfo _akkaCluster;
        private readonly Logger _logger;

        public sealed class MessageExtractor : IMessageExtractor
        {
            public string EntityId(object message) => (message as ShardEnvelope)?.EntityId;

            public string ShardId(object message) => (message as ShardEnvelope)?.ShardId;

            public object EntityMessage(object message) => (message as ShardEnvelope)?.Message;
        }
        
        public class SimpleClusterListener : UntypedActor
        {
            public static IReadOnlyCollection<Member> KnownMemberList => _knownMembers;
            public static IReadOnlyCollection<Member> KnownSeedsList => _knownSeedsMembers;

            protected ILoggingAdapter Log = Context.GetLogger();
            protected Akka.Cluster.Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);
            private static readonly List<Member> _knownMembers = new List<Member>();
            private static readonly List<Member> _knownSeedsMembers = new List<Member>();

            /// <summary>
            /// Need to subscribe to cluster changes
            /// </summary>
            protected override void PreStart()
            {
                // subscribe to IMemberEvent and UnreachableMember events
                Cluster.Subscribe(Self,
                                  ClusterEvent.InitialStateAsEvents,
                                  new[] {typeof(ClusterEvent.IMemberEvent), 
                                         typeof(ClusterEvent.UnreachableMember),
                                        });
            }

            /// <summary>
            /// Re-subscribe on restart
            /// </summary>
            protected override void PostStop()
            {
                Cluster.Unsubscribe(Self);
            }

            protected override void OnReceive(object message)
            {
                switch (message)
                {
                    case ClusterEvent.MemberUp up:
                        Log.Info("Member is Up: {0}", up.Member);
                        _knownMembers.Add(up.Member);
                        break;
                    case ClusterEvent.UnreachableMember unreachable:
                        Log.Info("Member detected as unreachable: {0}", unreachable.Member);
                        break;
                    case ClusterEvent.MemberRemoved removed:
                        Log.Info("Member is Removed: {0}", removed.Member);
                        break;
                    case ClusterEvent.IMemberEvent a:
                        var evt = a;
                        break;
                    case ClusterEvent.CurrentClusterState state:
                        _knownMembers.AddRange(state.Members);
                        break;
                    default:
                        Unhandled(message);
                        break;
                }
            }
        }

        public ClusterConfigurationTests(ITestOutputHelper output)
        {
            _logger = new XUnitAutoTestLoggerConfiguration(output).CreateLogger();
        }
        [Fact]
        public async Task Cluster_can_start_with_predefined_and_automatic_seed_nodes()
        {
            _akkaCluster = ActorSystemBuilder.New()
                                             .Cluster("testPredefined")
                                             .AutoSeeds(3)
                                             .Seeds(10010)
                                             .Workers(1)
                                             .Build()
                                             .CreateCluster(s => s.AttachSerilogLogging(_logger));
            
            await CheckClusterStarted(_akkaCluster);
        }

        [Fact(Skip = "Cannot make automatic nodes to register in diagnose actor")]
        public async Task Cluster_can_start_with_automatic_seed_nodes()
        {
            _akkaCluster = ActorSystemBuilder.New()
                                             .Cluster("testAutoSeed")
                                             .AutoSeeds(3)
                                             .Workers(1)
                                             .Build()
                                             .CreateCluster(s => s.AttachSerilogLogging(_logger));

            await CheckClusterStarted(_akkaCluster);
        }
        
        [Fact]
        public async Task Cluster_can_start_with_static_seed_nodes()
        {
            _akkaCluster = ActorSystemBuilder.New()
                                             .Cluster("testSeed")
                                             .Seeds(10011)
                                             .Workers(1)
                                             .Build()
                                             .CreateCluster(s => s.AttachSerilogLogging(_logger));


           await CheckClusterStarted(_akkaCluster);
        }

        private async Task CheckClusterStarted(ClusterInfo akkaCluster)
        {
            var diagnoseActor = akkaCluster.Cluster.System.ActorOf(Props.Create(() => new SimpleClusterListener()));

            //will give cluster time to form
            await Task.Delay(5000);

            var knownClusterMembers = SimpleClusterListener.KnownMemberList;
            var knownClusterAddresses = knownClusterMembers.Select(m => m.Address)
                                                           .ToArray();

            //All members of cluster should be reachable
            knownClusterAddresses.Should().BeEquivalentTo(akkaCluster.Members);

        }

       

        [Fact(Skip="Cannot understand why systems have problems with serializer config")]
        public async Task Cluster_can_host_an_actor_with_shard_region_with_predefined_seeds()
        {
            var clusterConfig = ActorSystemBuilder.New()
                                                  .Cluster("test")
                                                  .Seeds(10020)
                                                  .Workers(1)
                                                  .Build();
            
            _akkaCluster = clusterConfig.CreateCluster(s => s.AttachSerilogLogging(_logger));

// register actor type as a sharded entity
            var configs = clusterConfig.CreateConfigs();
            foreach(var cfg in configs)
                _logger.Warning(cfg);
            
            var actorSystem = _akkaCluster.Cluster.System;
            var region = await ClusterSharding.Get(actorSystem)
                                              .StartAsync(
                                                          typeName: "my-actor",
                                                          entityProps: Props.Create<EchoShardedActor>(),
                                                          settings: ClusterShardingSettings.Create(actorSystem),
                                                          messageExtractor: new MessageExtractor());
// send message to entity through shard region
            var message = "hello";
            var response = await region.Ask<object>(new ShardEnvelope("1", "1", message, MessageMetadata.Empty),
                                                    TimeSpan.FromSeconds(5));

            Assert.Equal(message.ToString(), response.ToString());
           // Dispose();
        }
                
        [Fact]
        public async Task Cluster_can_host_an_actor_with_shard_region_with_auto_seeds()
        {
            var clusterConfig = ActorSystemBuilder.New()
                                                  .Cluster("test")
                                                  .AutoSeeds(2)
                                                  .Workers(2)
                                                  .Build();
            
            _akkaCluster = clusterConfig.CreateCluster(s => s.AttachSerilogLogging(_logger));

            // register actor type as a sharded entity
            var configs = clusterConfig.CreateConfigs();
            foreach(var cfg in configs)
                _logger.Warning(cfg);
            
            var actorSystem = _akkaCluster.Cluster.System;
            var region = await ClusterSharding.Get(actorSystem)
                                              .StartAsync(
                                                          typeName: "my-actor",
                                                          entityProps: Props.Create<EchoShardedActor>(),
                                                          settings: ClusterShardingSettings.Create(actorSystem),
                                                          messageExtractor: new MessageExtractor());
            // send message to entity through shard region
            var message = "hello";
            var response = await region.Ask<object>(new ShardEnvelope("1", "1", message, MessageMetadata.Empty),
                                                    TimeSpan.FromSeconds(5));

            Assert.Equal(message, response.ToString());
        }
        
        [Fact(Skip="Cannot make this example work")]
        public async Task Cluster_can_host_an_actor_with_shard_region_simple()
        {
            Config config = @"
            akka {
              actor {
                provider = cluster
                serializers {
                  hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                }
                serialization-bindings {
                  ""System.Object"" = hyperion
                }
              }
              remote {
                dot-netty.tcp {
                  public-hostname = ""localhost""
                  hostname = ""localhost""
                  port = 0
                }
              }
              cluster {
                #auto-down-unreachable-after = 5s
                sharding {
                  least-shard-allocation-strategy.rebalance-threshold = 3
                }
              }
            }";
           
            var system = ActorSystem.Create("sharded-cluster-system", config.WithFallback(ClusterSingletonManager.DefaultConfig()));
            var cluster =  Akka.Cluster.Cluster.Get(system);
            cluster.JoinSeedNodes(new []{cluster.SelfAddress});
            
            for(int i = 0; i < 5; i++){
                
                system = ActorSystem.Create("sharded-cluster-system", config.WithFallback(ClusterSingletonManager.DefaultConfig()));
                system.AttachSerilogLogging(_logger);
                cluster.JoinSeedNodes(new []{((ExtendedActorSystem) system).Provider.DefaultAddress});
             }
                                            
                                            
            var region = await ClusterSharding.Get(system)
                                              .StartAsync(
                                                          typeName: "my-actor",
                                                          entityProps: Props.Create<EchoShardedActor>(),
                                                          settings: ClusterShardingSettings.Create(system),
                                                          messageExtractor: new MessageExtractor());
// send message to entity through shard region
            var message = "hello";
            var response = await region.Ask<object>(new ShardEnvelope("1", "1", message, MessageMetadata.Empty),
                                                    TimeSpan.FromSeconds(5));

            Assert.Equal(message.ToString(), response.ToString());
        }

        public void Dispose()
        {
            if (_akkaCluster == null) return;
            CoordinatedShutdown.Get(_akkaCluster?.Cluster.System)?.Run().Wait(TimeSpan.FromSeconds(2));
            _akkaCluster = null;
        }
    }
}