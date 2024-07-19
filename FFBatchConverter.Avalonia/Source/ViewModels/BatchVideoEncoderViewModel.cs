using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Threading;
using BidirectionalMap;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class BatchVideoEncoderViewModel : ReactiveObject
{

    private BiMap<VideoEncoder, EncoderTableRow> EncoderToRow { get; set; } = new BiMap<VideoEncoder, EncoderTableRow>();

    public ObservableCollection<EncoderTableRow> TableRows { get; set; } = [];

    [Reactive]
    public string Concurrency { get; set; } = "1";

    [Reactive]
    public string Subdirectory { get; set; } = "FFBatch";

    [Reactive]
    public string Extension { get; set; } = "mkv";

    [Reactive]
    public string FfmpegPath { get; set; } = Helpers.GetFFmpegPath() ?? "";

    [Reactive]
    public string FfprobePath { get; set; } = Helpers.GetFFprobePath() ?? "";

    [Reactive]
    public string Arguments { get; set; } = "-c:v libx265 -c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    private BatchVideoEncoder Encoder { get; }

    public BatchVideoEncoderViewModel()
    {
        Encoder = App.Instance.Encoder;
        Encoder.InformationUpdate += (sender, args) => Dispatcher.UIThread.Invoke(() => EncoderOnInformationUpdate(sender, args));

        // If these values change in the UI/ViewModel, we want to update the encoder with the new values.
        this
            .WhenAnyValue(x => x.Concurrency)
            .Subscribe(x => Encoder.Concurrency = int.TryParse(x, out int concurrency) ? concurrency : 1);
        this
            .WhenAnyValue(x => x.Subdirectory)
            .Subscribe(x => Encoder.OutputPath = x);
        this
            .WhenAnyValue(x => x.Extension)
            .Subscribe(x => Encoder.Extension = x);
        this
            .WhenAnyValue(x => x.FfmpegPath)
            .Subscribe(x => Encoder.FfmpegPath = x);
        this
            .WhenAnyValue(x => x.FfprobePath)
            .Subscribe(x => Encoder.FfprobePath = x);
        this
            .WhenAnyValue(x => x.Arguments)
            .Subscribe(x => Encoder.Arguments = x);
    }

    private void EncoderOnInformationUpdate(object? sender, InformationUpdateEventArgs e)
    {
        switch (e.ModificationType)
        {
            case DataModificationType.Add:
                TimeSpan duration = TimeSpan.FromSeconds(e.Encoder.Duration);
                EncoderTableRow row = new EncoderTableRow
                {
                    FileName = e.Encoder.InputFilePath,
                    Duration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}",
                    Size = $"{(new FileInfo(e.Encoder.InputFilePath).Length / 1024d / 1024):F2} MiB",
                    Status = e.Encoder.State.ToString()
                };

                TableRows.Add(row);
                EncoderToRow.Add(e.Encoder, row);
                break;
            case DataModificationType.Update:
                row = EncoderToRow.Forward[e.Encoder];
                row.Status = $"{e.Encoder.CurrentDuration / e.Encoder.Duration * 100:F2}%";

                if (e.Encoder.State is EncodingState.Error or EncodingState.Success)
                {
                    // Video encoder has finished
                    row.Status = e.Encoder.State.ToString();
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
}