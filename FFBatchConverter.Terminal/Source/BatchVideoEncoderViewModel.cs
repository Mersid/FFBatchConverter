using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Terminal.Gui;

namespace FFBatchConverter.Terminal;

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

    /// <summary>
    /// Null if no row is selected (table has no entries)
    /// </summary>
    [Reactive] public int? SelectedRow { get; set; }

    [Reactive]
    public string Footer { get; set; } = "";

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
        Encoder.InformationUpdate += (sender, args) => Application.Invoke(() => EncoderOnInformationUpdate(sender, args));

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

        this
            .WhenAnyValue(x => x.SelectedRow)
            .Subscribe(_ => UpdateFooter());
    }

    private void EncoderOnInformationUpdate(object? sender, InformationUpdateEventArgs e)
    {
        int preRowCount = RowsIndex.Count;
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

        // Track if this is the first time a row has been added to the table, as we need to handle this case.
        if (preRowCount == 0)
            SelectedRow = 0;
        UpdateFooter();

        // When the row is added to the table, it will be displayed on next update. This could include pressing a button,
        // scrolling, or any other event that triggers a redraw. However, we want it to automatically update as soon as the
        // data is added, so we increment the Reactor property to trigger the update. We set up a watch in the view that
        // observes this property and triggers an update when it changes.
        Reactor++;
    }

    private void UpdateFooter()
    {
        int? selectedIndex = SelectedRow;
        int total = RowsIndex.Count;
        double? scrollPercent = total switch
        {
            0 => null,
            1 => 100,
            _ => selectedIndex / (double)(total - 1) * 100
        };

        int pending = RowsIndex.Count(x => x.State == EncodingState.Pending);
        int encoding = RowsIndex.Count(x => x.State == EncodingState.Encoding);
        int success = RowsIndex.Count(x => x.State == EncodingState.Success);
        int error = RowsIndex.Count(x => x.State == EncodingState.Error);

        Footer = $"Selected: {(selectedIndex == null ? 0 : selectedIndex + 1)}/{total} ({(scrollPercent == null ? "--" : ((double)scrollPercent).ToString("F2"))}%) | Pending: {pending} | Encoding: {encoding} | Success: {success} | Error: {error}";
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