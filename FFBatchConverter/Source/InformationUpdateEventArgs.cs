namespace FFBatchConverter;

public class InformationUpdateEventArgs
{
    public required VideoEncoder Encoder { get; init; }
    public required DataModificationType ModificationType { get; init; }
}