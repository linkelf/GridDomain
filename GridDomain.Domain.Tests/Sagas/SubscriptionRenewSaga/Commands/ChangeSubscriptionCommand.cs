using GridDomain.CQRS;
using GridDomain.Tests.Sagas.SubscriptionRenewSaga.Events;

namespace GridDomain.Tests.Sagas.SubscriptionRenewSaga.Commands
{
    internal class ChangeSubscriptionCommand: Command
    {
        private NotEnoughFondsFailure e;

        public ChangeSubscriptionCommand(NotEnoughFondsFailure e)
        {
            this.e = e;
        }
    }
}