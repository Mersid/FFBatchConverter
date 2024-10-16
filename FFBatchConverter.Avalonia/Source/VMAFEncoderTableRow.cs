using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia;

public class VMAFEncoderTableRow : ReactiveObject
{
    [Reactive]
    public required string FileName { get; set; }

    [Reactive]
    public required string Duration { get; set; }

    [Reactive]
    public required string Size { get; set; }

    [Reactive]
    public required string Vmaf { get; set; }

    [Reactive]
    public required string Phase { get; set; }

    [Reactive]
    public required string Status { get; set; }
}