namespace FFBatchConverter;

public class InformationUpdateEventArgs
{
    public required VideoEncoder2 Encoder { get; init; }
    public required DataModificationType ModificationType { get; init; }
}