using System.Data;
using NStack;

namespace FFBatchConverter;

using Terminal.Gui;

public partial class MyView
{
	private DataTable FilesDataTable { get; set; }
	private List<VideoEncoder> Encoders { get; } = [];

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
		// selectedTextField.Text = (string)args.Table.Rows[args.NewRow][0];
	}

	private void OnStartButtonOnClicked()
	{
		foreach (VideoEncoder e in Encoders)
		{
			e.Start();
		}
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

	private void OnEncoderInfoUpdate(VideoEncoder encoder)
	{

		Application.MainLoop.Invoke(() =>
		{
			encoder.DataRow[3] = $"{encoder.CurrentDuration / encoder.Duration * 100:F2}%";

			SetNeedsDisplay();
		});

		// filesTableView.Update();
		// Driver.UpdateOffScreen ();
		// View last = this;
		// Rect r = new Rect(0, 0, 300, 300);
		// var v = last;
		//
		// 	if (v.Visible) {
		// 		// v.SetNeedsDisplay ();
		// 		Redraw (r);
		// 	}
		// Driver.Refresh ();
	}
}