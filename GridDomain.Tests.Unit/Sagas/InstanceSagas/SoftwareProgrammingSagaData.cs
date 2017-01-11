using System;
using GridDomain.EventSourcing.Sagas.InstanceSagas;

namespace GridDomain.Tests.Unit.Sagas.InstanceSagas
{
    public class SoftwareProgrammingSagaData : ISagaState
    {
        public Guid PersonId { get; set; }
        public string CurrentStateName { get; set; }
        public Guid CoffeeMachineId { get; }
        public Guid SofaId { get; set; }
        public Guid BadSleepPersonId { get; set; }

        public SoftwareProgrammingSagaData(string currentStateName,
                                           Guid sofaId = default(Guid),
                                           Guid coffeeMachineId = default(Guid),
                                           Guid personId = default(Guid))
        {
            SofaId = sofaId;
            CoffeeMachineId = coffeeMachineId;
            CurrentStateName = currentStateName;
            PersonId = personId;
        }
    }
}