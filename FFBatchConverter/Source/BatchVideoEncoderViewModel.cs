using System.ComponentModel;
using System.Data;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Terminal.Gui;

namespace FFBatchConverter;

public class BatchVideoEncoderViewModel : ReactiveObject
{
    /// <summary>
    /// We can't really make the DataTable reactive, so we'll just have to increment this whenever we add a new row.
    /// We can then set up a binding mechanism that watches this property and runs an update when it changes.
    /// Then, if we change this value every time we update FilesDataTable, we can have a reactive table.
    /// </summary>
    [Reactive] public int Reactor { get; private set; }
    private Dictionary<VideoEncoder, EncoderTableRow> EncoderToRow { get; set; } = new Dictionary<VideoEncoder, EncoderTableRow>();

    /// <summary>
    /// Matches by integer index to items in FilesDataTable
    /// </summary>
    private List<VideoEncoder> RowsIndex { get; set; } = [];

    public ListTableSource<EncoderTableRow> TableRows { get; } = new ListTableSource<EncoderTableRow>();

    [Reactive] public string Log { get; set; } = "";
    [Reactive] public int SelectedRow { get; set; }

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
        Encoder = ApplicationHost.Instance.Encoder;
        Encoder.InformationUpdate += EncoderOnInformationUpdate;

        // If these values change in the UI/ViewModel, we want to update the encoder with the new values.
        this
            .WhenAnyValue(x => x.Concurrency)
            .Subscribe(x => Encoder.Concurrency = int.TryParse(x, out int concurrency) ? concurrency : 1);
        this
            .WhenAnyValue(x => x.Arguments)
            .Subscribe(x => Encoder.Arguments = x);
        this
            .WhenAnyValue(x => x.Subdirectory)
            .Subscribe(x => Encoder.OutputPath = x);
        this
            .WhenAnyValue(x => x.Extension)
            .Subscribe(x => Encoder.Extension = x);

        // Update log
        this
            .WhenAnyValue(x => x.SelectedRow)
            .Subscribe(x =>
            {
                if (RowsIndex.Count == 0)
                    return;

                // VideoEncoder encoder = RowsIndex[x];
                // Log = encoder.Log.ToString();
            });
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
                RowsIndex.Add(e.Encoder);
                break;
            case DataModificationType.Update:
                row = EncoderToRow[e.Encoder];
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

        if (e.Encoder == RowsIndex[SelectedRow])
        {
            Log = e.Encoder.Log.ToString();
        }

        // When the row is added to the table, it will be displayed on next update. This could include pressing a button,
        // scrolling, or any other event that triggers a redraw. However, we want it to automatically update as soon as the
        // data is added, so we increment the Reactor property to trigger the update. We set up a watch in the view that
        // observes this property and triggers an update when it changes.
        Reactor++;
    }

    public void StartButtonPressed(object? sender, CancelEventArgs cancelEventArgs)
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

    public void AddFilesButtonPressed(object? sender, CancelEventArgs cancelEventArgs)
    {
        OpenDialog openDialog = new OpenDialog
        {
            AllowsMultipleSelection = true
        };
        Application.Run(openDialog);

        IReadOnlyList<string> dialogFilePaths = openDialog.FilePaths;

        Encoder.AddEntries(dialogFilePaths);
    }
}