using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using BidirectionalMap;
using FFBatchConverter.Controllers;
using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class BatchVMAFTargetEncoderViewModel : ReactiveObject
{
    private BiMap<VMAFTargetVideoEncoder, VMAFTargetEncoderTableRow> EncoderToRow { get; set; } = new BiMap<VMAFTargetVideoEncoder, VMAFTargetEncoderTableRow>();

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
    [Reactive]
    public string TargetVMAF { get; set; } = "86";

    [Reactive]
    public string Arguments { get; set; } = "-c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    private BatchVMAFTargetEncoder? Encoder { get; set; }

    public BatchVMAFTargetEncoderViewModel()
    {
        AttachEncoderEvents();
    }

    /// <summary>
    /// Fired when the encoder is rebuilt; likely because of settings change.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void EncoderRebuiltEvent()
    {
        Encoding = false;
        EncoderToRow = new BiMap<VMAFTargetVideoEncoder, VMAFTargetEncoderTableRow>();
        TableRows.Clear();
    }

    private void AttachEncoderEvents()
    {
        if (Encoder != null)
            Encoder.InformationUpdate -= EncoderOnInformationUpdate; // If already attached, remove the old event handler.
        Encoder = App.Instance.VMAFEncoder;
        Encoder.InformationUpdate += (sender, args) => Dispatcher.UIThread.Invoke(() => EncoderOnInformationUpdate(sender, args));
        App.Instance.EncoderRebuilt += EncoderRebuiltEvent;

        // If these values change in the UI/ViewModel, we want to update the encoder with the new values.
        this
            .WhenAnyValue(x => x.Concurrency)
            .Subscribe(x => Encoder.Concurrency = int.TryParse(x, out int concurrency) ? concurrency : 1);
        this
            .WhenAnyValue(x => x.Subdirectory)
            .Subscribe(x => Encoder.OutputSubdirectory = x);
        this
            .WhenAnyValue(x => x.Extension)
            .Subscribe(x => Encoder.Extension = x);
        this
            .WhenAnyValue(x => x.EncoderSelection)
            .Subscribe(x => Encoder.H265 = x == 0);
        this
            .WhenAnyValue(x => x.TargetVMAF)
            .Subscribe(x => Encoder.TargetVMAF = int.TryParse(x, out int concurrency) ? concurrency : 86);
        this
            .WhenAnyValue(x => x.Arguments)
            .Subscribe(x => Encoder.Arguments = x);
    }

    private void EncoderOnInformationUpdate(object? sender, InformationUpdateEventArgs<VMAFTargetEncoderStatusReport> e)
    {
        VMAFTargetVideoEncoder encoder = e.Report.Encoder;
        VMAFTargetEncoderStatusReport report = e.Report;

        switch (e.ModificationType)
        {
            case DataModificationType.Add:
                TimeSpan duration = TimeSpan.FromSeconds(encoder.Duration);
                VMAFTargetEncoderTableRow row = new VMAFTargetEncoderTableRow
                {
                    FileName = encoder.InputFilePath,
                    Duration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}",
                    Size = $"{encoder.FileSize / 1024d / 1024:F2} MiB",
                    Range = $"{report.LowCrf}-{report.HighCrf}",
                    Crf = report.ThisCrf.ToString(),
                    Vmaf = report.LastVMAF?.ToString("F2") ?? "-",
                    Phase = encoder.EncodingPhase.ToString(),
                    Status = report.State.ToString()
                };

                TableRows.Add(row);
                EncoderToRow.Add(encoder, row);
                break;
            case DataModificationType.Update:
                row = EncoderToRow.Forward[encoder];
                row.Range = $"{report.LowCrf}-{report.HighCrf}";
                row.Crf = report.ThisCrf.ToString();
                row.Vmaf = report.LastVMAF?.ToString("F2") ?? "-";
                row.Phase = encoder.EncodingPhase.ToString();
                row.Status = $"{report.CurrentDuration / encoder.Duration * 100:F2}%";

                if (report.State is EncodingState.Error or EncodingState.Success)
                {
                    // Video encoder has finished
                    row.Status = report.State.ToString();
                }

                break;
            default:
                throw new NotImplementedException();
        }
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
            Encoder.StopEncoding();
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
}