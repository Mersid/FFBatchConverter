using ReactiveUI;

namespace FFBatchConverter.Avalonia.ViewModels;

public class BatchVMAFTargetEncoderViewModel : ReactiveObject
{

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