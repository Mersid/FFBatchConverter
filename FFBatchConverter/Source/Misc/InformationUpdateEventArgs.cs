namespace FFBatchConverter.Misc;

public class InformationUpdateEventArgs<TEncoder>
{
    public required TEncoder Encoder { get; init; }
    public required DataModificationType ModificationType { get; init; }
}