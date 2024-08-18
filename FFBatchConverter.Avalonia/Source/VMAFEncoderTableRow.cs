using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia;

public class VMAFEncoderTableRow : ReactiveObject
{
    [Reactive]
    public string FileName { get; set; }

    [Reactive]
    public string Duration { get; set; }

    [Reactive]
    public string Size { get; set; }

    [Reactive]
    public string Vmaf { get; set; }

    [Reactive]
    public string Phase { get; set; }

    [Reactive]
    public string Status { get; set; }
}