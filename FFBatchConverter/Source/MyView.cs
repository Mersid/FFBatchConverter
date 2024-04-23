using System.Data;
using System.Diagnostics;
using NStack;

namespace FFBatchConverter;

using Terminal.Gui;

public partial class MyView
{
	private DataTable FilesDataTable { get; set; }
	private List<VideoEncoder> Encoders { get; } = [];
	private object EncodersLock { get; } = new object();
	private bool _running;

	private new bool Running
	{
		get => _running;
		set
		{
			Application.MainLoop.Invoke(() =>
			{
				_running = value;
				startButton.Width = 1; // Needed to force right-align when new text is smaller than the one at start.
				startButton.Text = Running ? "Stop" : "Start";
			});
		}
	}

	public MyView()
	{
		InitializeComponent();

		if (Helpers.GetFFmpegPath() is null)
		{
			MessageBox.ErrorQuery("FFmpeg not found!", "FFmpeg not found. If you have FFmpeg installed, make sure it's in the system PATH. Otherwise, you can manually specify the path to the program.", 1, "Continue");
		}

		if (Helpers.GetFFprobePath() is null)
		{
			MessageBox.ErrorQuery("FFprobe not found!", "FFprobe not found. If you have FFprobe installed, make sure it's in the system PATH. Otherwise, you can manually specify the path to the program.", 1, "Continue");
		}

		addFilesButton.Clicked += OnAddFilesButtonOnClicked;
		startButton.Clicked += OnStartButtonOnClicked;

		filesTableView.SelectedCellChanged += OnFilesTableViewOnSelectedCellChanged;
	}

	private void OnFilesTableViewOnSelectedCellChanged(TableView.SelectedCellChangedEventArgs args)
	{
		logTextView.Text = Encoders[args.NewRow].Log.ToString();
	}

	private void OnStartButtonOnClicked()
	{
		// Kick-start the encoding process. As soon as the first video receives update data or is instantly completed/failed,
		// an event loop will trigger the next video to start encoding. We can't do that here, because it would cause
		// a race condition.
		Encoders.FirstOrDefault(t => t.State == EncodingState.Pending)?.Start(commandTextField.Text.ToString(), subdirectoryTextField.Text.ToString(), extensionTextField.Text.ToString());

		if (Encoders.Count == 0)
			return;

		Running = !Running;
	}

	private void OnAddFilesButtonOnClicked()
	{
		OpenDialog openDialog = new OpenDialog("Open File", "Choose a file to open") { CanChooseDirectories = true, CanChooseFiles = true, AllowsMultipleSelection = true, };

		Application.Run(openDialog);

		IReadOnlyList<string> paths = openDialog.FilePaths;

		if (openDialog.Canceled)
			return;

		foreach (string path in paths)
		{
			if (!File.Exists(path))
				continue;

			DataRow row = FilesDataTable.NewRow();
			FilesDataTable.Rows.Add(row);

			VideoEncoder e = new VideoEncoder(path, row);
			e.InfoUpdate += OnEncoderInfoUpdate;
			Encoders.Add(e);
		}

		filesTableView.Update();
	}

	private void OnEncoderInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? e)
	{
		// Update gui display
		Application.MainLoop.Invoke(() =>
		{
			encoder.DataRow[3] = $"{encoder.CurrentDuration / encoder.Duration * 100:F2}%";

			if (encoder.State is EncodingState.Error or EncodingState.Success)
			{
				// Video encoder has finished
				encoder.DataRow[3] = encoder.State.ToString();
			}

			if (Encoders[filesTableView.SelectedRow] == encoder)
			{
				// If the update occurred from the selected encoder, write the new line to the log.
				// Selecting must be set to false, or the new line will overwrite the selected contents.
				// The InsertText() command writes to the end of the text view, so we need to move the cursor there first.
				// We also need to set ReadOnly to false before writing, and then back to true after writing.
				logTextView.Selecting = false;
				logTextView.MoveEnd();
				logTextView.ReadOnly = false;
				logTextView.InsertText(e?.Data + "\n" ?? "");
				logTextView.ReadOnly = true;
			}

			SetNeedsDisplay();
		});

		// This could be called from different threads, but if an encoder gets started twice, the program will crash.
		lock (EncodersLock)
		{
			// Begin encoding videos if current count allows it
			bool result = int.TryParse(concurrencyTextField.Text.ToString(), out int concurrency);

			if (!result)
				concurrency = 1;

			if (Running && Encoders.Count(e => e.State == EncodingState.Encoding) < concurrency)
			{
				VideoEncoder? next = Encoders.FirstOrDefault(e => e.State == EncodingState.Pending);
				next?.Start(commandTextField.Text.ToString(), subdirectoryTextField.Text.ToString(), extensionTextField.Text.ToString());
			}

			if (Encoders.Count(e => e.State == EncodingState.Encoding) == 0)
			{
				Running = false;
			}
		}
	}
}