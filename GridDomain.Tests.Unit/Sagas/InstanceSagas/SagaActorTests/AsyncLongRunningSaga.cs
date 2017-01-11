using System.Threading.Tasks;
using Automatonymous;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Tests.Unit.SampleDomain.Events;

namespace GridDomain.Tests.Unit.Sagas.InstanceSagas
{
    public class AsyncLongRunningSaga : Saga<TestState>
    {
        public static ISagaDescriptor Descriptor
        {
            get
            {
                var descriptor = SagaDescriptor.CreateDescriptor<AsyncLongRunningSaga, TestState>();
                descriptor.AddStartMessage<SampleAggregateCreatedEvent>();
                return descriptor;
            }
        }

        public AsyncLongRunningSaga()
        {
            InstanceState(s => s.CurrentStateName);

            During(Initial,
                When(Start).ThenAsync(async ctx =>
                {
                    ctx.Instance.ProcessingId = ctx.Data.SourceId;
                    await Task.Delay(100);
                })
               .TransitionTo(Final));

        }

        public Event<SampleAggregateCreatedEvent> Start { get; private set; }
    }
}