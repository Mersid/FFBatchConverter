using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia;

public class EncoderTableRow : ReactiveObject
{
    [Reactive]
    public required string FileName { get; set; }

    [Reactive]
    public required string Duration { get; set; }

    [Reactive]
    public required string Size { get; set; }

    [Reactive]
    public required string Status { get; set; }
}