using System.Collections.Generic;
using System.Collections.ObjectModel;
using BidirectionalMap;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class BatchVMAFTargetEncoderViewModel : ReactiveObject
{
    private BiMap<VideoEncoder, VMAFTargetEncoderTableRow> EncoderToRow { get; set; } = new BiMap<VideoEncoder, VMAFTargetEncoderTableRow>();

    public ObservableCollection<VMAFTargetEncoderTableRow> TableRows { get; set; } = [];

    [Reactive]
    public string Concurrency { get; set; } = "1";

    [Reactive]
    public string Subdirectory { get; set; } = "FFBatch";

    [Reactive]
    public string Extension { get; set; } = "mkv";

    /// <summary>
    /// 0 is x265, 1 is x264, since it's by index, and we put x265 at the top.
    /// </summary>
    [Reactive]
    public int EncoderSelection { get; set; } = 0;

    // TODO: Add verification.
    public double TargetVMAF { get; set; } = 86;

    [Reactive]
    public string Arguments { get; set; } = "-c:v libx265 -c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    private BatchVMAFTargetEncoder Encoder { get; set; }

    public BatchVMAFTargetEncoderViewModel()
    {
        AttachEncoderEvents();
    }

    private void AttachEncoderEvents()
    {
        // TODO: Attach encoder events.
        Encoder = App.Instance.VMAFEncoder;
    }

    public void StartButtonClicked()
    {
        Encoding = !Encoding;

        if (Encoding)
        {
            Encoder.StartEncoding();
        }
        else
        {
            Encoder.StartEncoding();
        }
    }

    /// <summary>
    /// Adds files to the encoder.
    /// Directories will be recursively added on the encoder side.
    /// </summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        Encoder.AddEntries(paths);
    }

    public void DoTheNeedful()
    {
        Encoder.OutputSubdirectory = "FFBatch";
        Encoder.H265 = false;
        Encoder.Concurrency = 1;
        Encoder.TargetVMAF = 86;
        Encoder.Extension = "mkv";
        Encoder.Arguments = "-c:a aac";
        Encoder.AddEntries(new[] {"C:\\Users\\Admin\\Workshop\\FFBatchConverter\\FFBatchConverter.Avalonia\\bin\\Debug\\test2.mp4"});
        Encoder.StartEncoding();
        int t = 8;
    }

    public void ExtraButton()
    {
        int y = 8;
    }
}