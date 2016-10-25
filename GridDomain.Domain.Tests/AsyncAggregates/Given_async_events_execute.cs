using System;
using System.Threading;
using GridDomain.CQRS;
using GridDomain.Tests.CommandsExecution;
using GridDomain.Tests.SampleDomain;
using GridDomain.Tests.SampleDomain.Commands;
using GridDomain.Tests.SampleDomain.Events;
using NUnit.Framework;

namespace GridDomain.Tests.AsyncAggregates
{
    [TestFixture]
    public class Given_async_events_execute : InMemorySampleDomainTests
    {

        [Test]
        public void When_async_method_is_called_domainEvents_are_persisted()
        {
            var cmd = new AsyncMethodCommand(43, Guid.NewGuid(),Guid.Empty,TimeSpan.FromMilliseconds(50));
            var expect = Expect.Message<SampleAggregateChangedEvent>();
            GridNode.ExecuteSync(cmd,Timeout, expect);
            var aggregate = LoadAggregate<SampleAggregate>(cmd.AggregateId);

            Assert.AreEqual(cmd.Parameter.ToString(), aggregate.Value);
        }
    }
}