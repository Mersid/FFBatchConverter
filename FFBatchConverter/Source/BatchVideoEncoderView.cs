using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using Terminal.Gui;

namespace FFBatchConverter;

[Obsolete("Use newer version based on MVVM architecture instead.")]
public partial class BatchVideoEncoderView : View
{
    // Suppress null as it is initialized in the constructor in a separate method call.
	private DataTable FilesDataTable { get; set; } = null!;
	private List<VideoEncoder> Encoders { get; } = [];
	private object EncodersLock { get; } = new object();
	private bool _running;

	private bool Running
	{
		get => _running;
		set
		{
			Application.Invoke(() =>
			{
				_running = value;
				startButton.Text = Running ? "Stop" : "Start";
			});
		}
	}

	public BatchVideoEncoderView()
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

		addFilesButton.Accept += OnAddFilesButtonOnClicked;
		startButton.Accept += OnStartButtonOnClicked;
		aboutButton.Accept += (_, _) => MessageBox.Query("About", "Version 1.0.0\n" +
		                                                      "By Mersid\n" +
		                                                      "https://github.com/Mersid/FFBatchConverter\n" +
		                                                      "This program is released in the hope that it will be useful.", "Continue");

		filesTableView.SelectedCellChanged += OnFilesTableViewOnSelectedCellChanged;
	}

	private void OnFilesTableViewOnSelectedCellChanged(object? sender, SelectedCellChangedEventArgs args)
	{
		logTextView.Text = Encoders[args.NewRow].Log.ToString();
	}

	private void OnStartButtonOnClicked(object? sender, CancelEventArgs cancelEventArgs)
	{
		// Kick-start the encoding process. As soon as the first video receives update data or is instantly completed/failed,
		// an event loop will trigger the next video to start encoding. We can't do that here, because it would cause
		// a race condition.
		lock (EncodersLock)
		{
			// Do nothing if there's no videos to encode
			if (Encoders.Count == 0)
				return;

			// Begin encoding videos if current count allows it
			bool result = int.TryParse(concurrencyTextField.Text, out int concurrency);

			if (!result)
				concurrency = 1;

			if (Encoders.Count(e2 => e2.State == EncodingState.Encoding) < concurrency)
				Encoders.FirstOrDefault(t => t.State == EncodingState.Pending)?.Start(argumentsTextField.Text!, subdirectoryTextField.Text!, extensionTextField.Text!);

			Running = !Running;
		}
	}

	private void OnAddFilesButtonOnClicked(object? sender, CancelEventArgs cancelEventArgs)
	{
		OpenDialog openDialog = new OpenDialog
		{
			AllowsMultipleSelection = true
		};
		Application.Run(openDialog);

		IReadOnlyList<string> paths = openDialog.FilePaths;

		// For folders, we need to get all files in the folder. For now, non-recursive.
		List<string> paths2 = [];

		foreach (string path in paths)
		{
			if (Directory.Exists(path))
			{
				paths2.AddRange(Directory.GetFiles(path));
			}
			else
			{
				paths2.Add(path);
			}
		}

		if (openDialog.Canceled)
			return;

		foreach (string path in paths2)
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
		Application.Invoke(() =>
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
				logTextView.InsertText(e?.Data + "\n");
				logTextView.ReadOnly = true;
			}

			SetNeedsDisplay();
		});

		// This could be called from different threads, but if an encoder gets started twice, the program will crash.
		lock (EncodersLock)
		{
			// Begin encoding videos if current count allows it
			bool result = int.TryParse(concurrencyTextField.Text, out int concurrency);

			if (!result)
				concurrency = 1;

			if (Running && Encoders.Count(e2 => e2.State == EncodingState.Encoding) < concurrency)
			{
				VideoEncoder? next = Encoders.FirstOrDefault(e2 => e2.State == EncodingState.Pending);
				next?.Start(argumentsTextField.Text!, subdirectoryTextField.Text!, extensionTextField.Text!);
			}

			if (Encoders.Count(e2 => e2.State == EncodingState.Encoding) == 0)
			{
				Running = false;
			}
		}
	}
}