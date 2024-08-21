using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Threading;
using BidirectionalMap;
using DynamicData;
using FFBatchConverter.Controllers;
using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class BatchVideoEncoderViewModel : ReactiveObject
{

    public BiMap<VideoEncoder, EncoderTableRow> EncoderToRow { get; set; } = new BiMap<VideoEncoder, EncoderTableRow>();

    public ObservableCollection<EncoderTableRow> TableRows { get; set; } = [];

    [Reactive]
    public string Concurrency { get; set; } = "1";

    [Reactive]
    public string Subdirectory { get; set; } = "FFBatch";

    [Reactive]
    public string Extension { get; set; } = "mkv";

    [Reactive]
    public string Arguments { get; set; } = "-c:v libx265 -c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    private BatchVideoEncoder? Encoder { get; set; }

    public BatchVideoEncoderViewModel()
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
        EncoderToRow = new BiMap<VideoEncoder, EncoderTableRow>();
        TableRows.Clear();
    }

    private void AttachEncoderEvents()
    {
        if (Encoder != null)
            Encoder.InformationUpdate -= EncoderOnInformationUpdate; // If already attached, remove the old event handler.
        Encoder = App.Instance.Encoder;
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
            .WhenAnyValue(x => x.Arguments)
            .Subscribe(x => Encoder.Arguments = x);
    }

    private void EncoderOnInformationUpdate(object? sender, InformationUpdateEventArgs<VideoEncoderStatusReport> e)
    {
        VideoEncoder encoder = e.Report.Encoder;
        VideoEncoderStatusReport report = e.Report;

        switch (e.ModificationType)
        {
            case DataModificationType.Add:
                TimeSpan duration = TimeSpan.FromSeconds(encoder.Duration);
                EncoderTableRow row = new EncoderTableRow
                {
                    FileName = encoder.InputFilePath,
                    Duration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}",
                    Size = $"{encoder.FileSize / 1024d / 1024:F2} MiB",
                    Status = report.State.ToString()
                };

                TableRows.Add(row);
                EncoderToRow.Add(encoder, row);
                break;
            case DataModificationType.Update:
                row = EncoderToRow.Forward[encoder];
                row.Status = $"{report.CurrentDuration / encoder.Duration * 100:F2}%";

                if (report.State is EncodingState.Error or EncodingState.Success)
                {
                    // Video encoder has finished
                    row.Status = report.State.ToString();
                }

                break;
            case DataModificationType.Remove:
                // Nothing to do here.
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.ModificationType), "Unknown modification type.");
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

    public void RemoveEncodersByRow(IEnumerable<EncoderTableRow> rows)
    {
        // Filter encoders that are currently encoding.
        IEnumerable<VideoEncoder> encoders = rows
            .Select(t => EncoderToRow.Reverse[t])
            .Where(t => t.Report.State is not EncodingState.Encoding);

        Encoder.RemoveEntries(encoders);
        foreach (VideoEncoder encoder in encoders)
        {
            EncoderTableRow row = EncoderToRow.Forward[encoder];
            EncoderToRow.Remove(encoder);
            TableRows.Remove(row);
        }
    }

    public void ResetEncodersByRow(IEnumerable<EncoderTableRow> rows)
    {
        // TODO: Redo/junk this.
        IEnumerable<VideoEncoder> encoders = rows
            .Select(t => EncoderToRow.Reverse[t])
            .Where(t => t.Report.State is not EncodingState.Encoding);

        foreach (VideoEncoder oldEncoder in encoders)
        {
            TimeSpan duration = TimeSpan.FromSeconds(oldEncoder.Duration);
            EncoderTableRow newRow = new EncoderTableRow
            {
                FileName = oldEncoder.InputFilePath,
                Duration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}",
                Size = $"{oldEncoder.FileSize / 1024d / 1024:F2} MiB",
                Status = oldEncoder.Report.State.ToString()
            };

            EncoderTableRow oldRow = EncoderToRow.Forward[oldEncoder];

            TableRows.Replace(oldRow, newRow);
            EncoderToRow.Remove(oldEncoder);

            Encoder.RemoveEntries([oldEncoder]);

            EncoderToRow.Add(oldEncoder, newRow);
        }
    }
}