using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.DependencyInjection.FutureEvents
{
    public class FutureEventsTest : NodeTestKit
    {
        public FutureEventsTest(ITestOutputHelper output) : base(output, new FutureEventsFixture()) {}
    }
}