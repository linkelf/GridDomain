using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.Node.Actors.CommandPipe.Processors;

namespace GridDomain.Node.Actors.CommandPipe {
    public static class ProcessorListCatalogExtensions
    {
        public static Task ProcessMessage(this IProcessorListCatalog processorListCatalog, IMessageMetadataEnvelop envelop)
        {
            var processors = processorListCatalog.Get(envelop.Message);
            return processors?.Aggregate<IMessageProcessor, Task>(null,
                                                          (workInProgress, nextProcessor) => nextProcessor.Process(envelop, workInProgress))
                   ?? Task.CompletedTask;
        }

        public static Task<T[]> ProcessMessage<T>(this IProcessorListCatalog<T> processorListCatalog, IMessageMetadataEnvelop envelop)
        {
            var processors = processorListCatalog.Get(envelop.Message);
            Task finalTask = Task.CompletedTask;
            var results = processors.Select(p => p.Process(envelop.Message, ref finalTask)).ToArray();
            return finalTask.ContinueWith(t => results.Select(r => r.Result).
                                                       ToArray());
        }
    }
}