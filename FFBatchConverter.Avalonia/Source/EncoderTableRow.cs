using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia;

public class EncoderTableRow : ReactiveObject
{
    [Reactive]
    public string FileName { get; set; }

    [Reactive]
    public string Duration { get; set; }

    [Reactive]
    public string Size { get; set; }

    [Reactive]
    public string Status { get; set; }
}