namespace FFBatchConverter.Misc;

public class InformationUpdateEventArgs<TReport>
{
    public required TReport Report { get; init; }
    public required DataModificationType ModificationType { get; init; }
}