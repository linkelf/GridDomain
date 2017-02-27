using System;
using GridDomain.CQRS;

namespace GridDomain.Tests.XUnit.EventsUpgrade.Domain.Commands
{
    public class CreateBalanceCommand : Command
    {
        public CreateBalanceCommand(int parameter, Guid aggregateId):base(aggregateId)
        {
            Parameter = parameter;
        }

        public int Parameter { get; }
    }
}