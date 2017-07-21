using System;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.Configuration.MessageRouting;
using GridDomain.CQRS;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution {
    public class When_awaiting_command_execution_without_prepare_and_counting : NodeTestKit
    {
        class BalloonCountingDomainConfiguration : IDomainConfiguration
        {
            public void Register(IDomainBuilder builder)
            {
                builder.RegisterAggregate(new BalloonDependencyFactory());
                builder.RegisterHandler<BalloonCreated, CountingMessageHandler>().AsParallel();
                builder.RegisterHandler<BalloonCreated, SlowCountingMessageHandler>(c => new SlowCountingMessageHandler(c.Publisher)).AsFireAndForget();
                builder.RegisterHandler<BalloonTitleChanged, CountingMessageHandler>().AsSync();
            }
        }

        class CountingMessageHandler : IHandler<BalloonCreated>,
                                       IHandler<BalloonTitleChanged>
        {
            public static int CreatedCount;
            public static int ChangedCount;
            public Task Handle(BalloonCreated message, IMessageMetadata metadata = null)
            {
                CreatedCount++;
                return Task.Delay(50);
            }

            public Task Handle(BalloonTitleChanged message, IMessageMetadata metadata = null)
            {
                ChangedCount++;
                return Task.Delay(15);
            }
        }
        class SlowCountingMessageHandler : IHandler<BalloonCreated>
        {
            public SlowCountingMessageHandler(IPublisher publisher)
            {
                _publisher = publisher;
            }
            public static int CreatedCount;
            private readonly IPublisher _publisher;

            public async Task Handle(BalloonCreated message, IMessageMetadata metadata = null)
            {
                await Task.Delay(500);
                _publisher.Publish(500);
                CreatedCount++;
            }
        }

        public When_awaiting_command_execution_without_prepare_and_counting(ITestOutputHelper output) :
            base(output, new NodeTestFixture(new BalloonCountingDomainConfiguration())){ }

        [Fact]
        public async Task Then_command_executed_sync_and_parralel_message_processor_are_executed()
        {
            var aggregateId = Guid.NewGuid();
            var slowCounterWaiter = Node.NewWaiter().Expect<int>().Create();
            //will produce one created message and two title changed
            await Node.Execute(new InflateCopyCommand(100, aggregateId),
                               new WriteTitleCommand(200, aggregateId));

            Assert.Equal(1, CountingMessageHandler.CreatedCount);
            Assert.Equal(2, CountingMessageHandler.ChangedCount);
            //will not wait antil Fire and Forget handlers
            Assert.Equal(0, SlowCountingMessageHandler.CreatedCount);
            await slowCounterWaiter;
            //but Fire and Forget handler was launched and will complete later
            Assert.Equal(1, SlowCountingMessageHandler.CreatedCount);
        }

    }
}