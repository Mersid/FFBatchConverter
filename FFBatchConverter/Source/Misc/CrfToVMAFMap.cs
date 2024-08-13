namespace FFBatchConverter.Misc;

public class CrfToVMAFMap
{
    public required string FilePath { get; init; }
    public required int Crf { get; init; }
    public required double VmafScore { get; init; }
}