using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using BidirectionalMap;
using FFBatchConverter.Controllers;
using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;
using FFBatchConverter.Tokens;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class BatchVMAFTargetEncoderViewModel : ReactiveObject
{
    public BiMap<VMAFTargetEncoderToken, VMAFTargetEncoderTableRow> EncoderToRow { get; set; } = new BiMap<VMAFTargetEncoderToken, VMAFTargetEncoderTableRow>();

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

    public BatchVMAFTargetEncoderViewModel(TaskCreateInfo createInfo)
    {
        Encoder = new BatchVMAFTargetEncoder
        {
            FFmpegPath = createInfo.FFmpegPath,
            FFprobePath = createInfo.FFprobePath,
            TempDirectory = createInfo.TempDirectory
        };

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
            .Subscribe(x => Encoder.TargetVMAF = int.TryParse(x, out int targetVMAF) ? targetVMAF : 86);
        this
            .WhenAnyValue(x => x.Arguments)
            .Subscribe(x => Encoder.Arguments = x);
    }

    /// <summary>
    /// Fired when the encoder is rebuilt; likely because of settings change.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void EncoderRebuiltEvent()
    {
        Encoding = false;
        EncoderToRow = new BiMap<VMAFTargetEncoderToken, VMAFTargetEncoderTableRow>();
        TableRows.Clear();
    }

    private void EncoderOnInformationUpdate(object? sender, InformationUpdateEventArgs<VMAFTargetEncoderStatusReport> e)
    {
        VMAFTargetEncoderToken token = e.Report.Token;
        VMAFTargetEncoderStatusReport report = e.Report;

        switch (e.ModificationType)
        {
            case DataModificationType.Add:
                TimeSpan duration = TimeSpan.FromSeconds(report.Duration);
                VMAFTargetEncoderTableRow row = new VMAFTargetEncoderTableRow
                {
                    FileName = report.InputFilePath,
                    Duration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}",
                    Size = $"{report.FileSize / 1024d / 1024:F2} MiB",
                    Range = $"{report.LowCrf}-{report.HighCrf}",
                    Crf = report.ThisCrf.ToString(),
                    Vmaf = report.LastVMAF?.ToString("F2") ?? "-",
                    Phase = report.EncodingPhase.ToString(),
                    Status = report.State.ToString()
                };

                TableRows.Add(row);
                EncoderToRow.Add(token, row);
                break;
            case DataModificationType.Update:
                row = EncoderToRow.Forward[token];
                row.Range = $"{report.LowCrf}-{report.HighCrf}";
                row.Crf = report.ThisCrf.ToString();
                row.Vmaf = report.LastVMAF?.ToString("F2") ?? "-";
                row.Phase = report.EncodingPhase.ToString();
                row.Status = report.State is EncodingState.Encoding ? $"{report.CurrentDuration / report.Duration * 100:F2}%" : report.State.ToString();

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

    public void RemoveEncodersByRow(IEnumerable<VMAFTargetEncoderTableRow> rows)
    {
        List<VMAFTargetEncoderToken> tokens = rows
            .Select(t => EncoderToRow.Reverse[t])
            .Where(t => Encoder.GetReport(t).State is not EncodingState.Encoding)
            .ToList(); // If this isn't here, the RemoveEntries call will cause an exception when enumerating in the foreach loop below.

        Encoder.RemoveEntries(tokens);

        foreach (VMAFTargetEncoderToken token in tokens)
        {
            VMAFTargetEncoderTableRow row = EncoderToRow.Forward[token];
            TableRows.Remove(row);
            EncoderToRow.Remove(token);
        }
    }

    public void ResetEncodersByRow(IEnumerable<VMAFTargetEncoderTableRow> rows)
    {
        List<VMAFTargetEncoderToken> tokens = rows
            .Select(t => EncoderToRow.Reverse[t])
            .Where(t => Encoder.GetReport(t).State is not EncodingState.Encoding)
            .ToList(); // If this isn't here, the RemoveEntries call will cause an exception when enumerating in the foreach loop below.

        Encoder.ResetEntries(tokens);
    }

    public string GetLogs(VMAFTargetEncoderToken token)
    {
        return Encoder.GetLogs(token);
    }
}