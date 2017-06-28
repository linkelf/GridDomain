using System;
using Akka.Actor;
using Akka.TestKit.TestActors;
using GridDomain.Common;
using GridDomain.EventSourcing.Sagas;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain.Configuration;
using Microsoft.Practices.Unity;

namespace GridDomain.Tests.Acceptance.EventsUpgrade
{
    public static class TestFixtureExtensions
    {
        public static void InitFastRecycle(this NodeTestFixture fixture,
                                           TimeSpan? clearPeriod = null,
                                           TimeSpan? maxInactiveTime = null)
        {
            fixture.Add(
                        new ContainerConfiguration(c =>c.RegisterInstance<IPersistentChildsRecycleConfiguration>(new PersistentChildsRecycleConfiguration(clearPeriod ?? TimeSpan.FromMilliseconds(200),
                                                                                                                                                                maxInactiveTime ?? TimeSpan.FromMilliseconds(50)))));
        }

        public static NodeTestFixture InitSampleAggregateSnapshots(this NodeTestFixture fixture,
                                                                   int keep = 1,
                                                                   TimeSpan? maxSaveFrequency = null,
                                                                   int saveOnEach = 1)
        {
            var aggregateDependencyFactory = DefaultAggregateDependencyFactory.New(new BalloonCommandHandler());
            aggregateDependencyFactory.SnapshotPolicyCreator = () => new SnapshotsPersistencePolicy(saveOnEach, keep, maxSaveFrequency);
            fixture.Add(new DomainConfiguration(d => d.RegisterAggregate(aggregateDependencyFactory)));

            return fixture;
        }

        public static NodeTestFixture InitSoftwareProgrammingSagaSnapshots(this NodeTestFixture fixture,
                                                                           int keep = 1,
                                                                           TimeSpan? maxSaveFrequency = null,
                                                                           int saveOnEach = 1)
        {
            var sagaDependencies = new SoftwareProgrammingSagaDependenciesFactory(fixture.Logger);
            sagaDependencies.StateDependencyFactory.SnapshotPolicyCreator = () => new SnapshotsPersistencePolicy(saveOnEach,keep,maxSaveFrequency);

            fixture.Add(new DomainConfiguration(d => d.RegisterSaga(sagaDependencies)));
            
            return fixture;
        }

        public static NodeTestFixture IgnoreCommands(this NodeTestFixture fixture)
        {
            fixture.OnNodeStartedEvent += (sender, e) =>
                                          {
                                              //supress errors raised by commands not reaching aggregates
                                              var nullActor = fixture.Node.System.ActorOf(BlackHoleActor.Props);
                                              fixture.Node.Pipe.SagaProcessor.Ask<Initialized>(new Initialize(nullActor))
                                                     .Wait();
                                          };

            return fixture;
        }
    }
}