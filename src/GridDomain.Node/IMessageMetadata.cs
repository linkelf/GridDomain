namespace GridDomain.Node
{
    public interface IMessageMetadata
    {
        string CasuationId { get; }
        string CorrelationId { get; }
        string MessageId { get; }
    }
}