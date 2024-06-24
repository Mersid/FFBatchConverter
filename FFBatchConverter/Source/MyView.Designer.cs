using System.Data;

namespace FFBatchConverter;

using System;
using Terminal.Gui;


public partial class MyView : Window
{
	private View container;
	private TableView filesTableView;
	private TextView logTextView;

	private Label concurrencyLabel;
	private TextField concurrencyTextField;

	private Label subdirectoryLabel;
	private TextField subdirectoryTextField;

	private Label extensionLabel;
	private TextField extensionTextField;

	private Label ffmpegPathLabel;
	private TextField ffmpegPathTextField;

	private Label ffprobePathLabel;
	private TextField ffprobePathTextField;

	private Label argumentsLabel;
	private TextField argumentsTextField;

	private Button startButton;
	private Button addFilesButton;
	private Button aboutButton;

	private void InitializeComponent()
	{
		this.Width = Dim.Fill(0);
		this.Height = Dim.Fill(0);
		this.X = 0;
		this.Y = 0;
		this.Modal = false;
		this.Text = "";
		this.Border.BorderStyle = LineStyle.Single;
		this.TextAlignment = Alignment.Start;
		this.Title = "Press Ctrl+Q to quit";
		this.ColorScheme = new ColorScheme(this.ColorScheme)
		{
			Normal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
		};


		// Holds the list and text box
		this.container = new View
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 4,
			// ColorScheme = new ColorScheme
			// {
			// 	Normal = Application.Driver.MakeAttribute(Color.Cyan, Color.Green),
			// }
		};

		FilesDataTable = new DataTable();
		FilesDataTable.Columns.Add("File name", typeof(string));
		FilesDataTable.Columns.Add("Duration", typeof(string));
		FilesDataTable.Columns.Add("Size", typeof(string));
		FilesDataTable.Columns.Add("Status", typeof(string));

		this.filesTableView = new TableView
		{
			Width = Dim.Fill(),
			Height = Dim.Percent(50),
			X = 0,
			Y = 0,
			FullRowSelect = true,
			MultiSelect = false,
			Table = new DataTableSource(FilesDataTable),
			// ColorScheme = new ColorScheme
			// {
			// 	Normal = Application.Driver.MakeAttribute(Color.Cyan, Color.Green),
			// }
		};

		this.logTextView = new TextView
		{
			X = 0,
			Y = Pos.Bottom(filesTableView),
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			Text = "",
			ReadOnly = true
		};

		this.concurrencyLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(4),
			Text = "Concurrency",
		};

		this.concurrencyTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(4),
			Width = 3,
			Height = 1,
			Text = "1",
		};

		this.subdirectoryLabel = new Label
		{
			X = 20,
			Y = Pos.AnchorEnd(4),
			Text = "Subdir",
		};

		this.subdirectoryTextField = new TextField
		{
			X = 28,
			Y = Pos.AnchorEnd(4),
			Width = 30,
			Height = 1,
			Text = "FFBatch",
		};

		this.extensionLabel = new Label
		{
			X = 61,
			Y = Pos.AnchorEnd(4),
			Text = "Extension",
		};

		this.extensionTextField = new TextField
		{
			X = 72,
			Y = Pos.AnchorEnd(4),
			Width = 6,
			Height = 1,
			Text = "mkv",
		};

		this.ffmpegPathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(3),
			Text = "FFmpeg Path",
		};

		this.ffmpegPathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(3),
			Width = 64,
			Text = Helpers.GetFFmpegPath() ?? "",
			Height = 1,
		};

		this.ffprobePathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(2),
			Text = "FFprobe Path",
		};

		this.ffprobePathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(2),
			Width = 64,
			Text = Helpers.GetFFprobePath() ?? "",
			Height = 1,
		};

		this.argumentsLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(1),
			Text = "Arguments",
		};

		this.argumentsTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(1),
			Width = 64,
			Height = 1,
			Text = "-c:v libx265 -c:a aac"
		};

		this.startButton = new Button
		{
			X = Pos.AnchorEnd(),
			Y = Pos.AnchorEnd(3),
			Text = "Start",
			TextAlignment = Alignment.Center,
		};

		this.addFilesButton = new Button
		{
			X = Pos.AnchorEnd(),
			Y = Pos.AnchorEnd(2),
			Text = "Add files",
			TextAlignment = Alignment.Center,
		};

		this.aboutButton = new Button
		{
			X = Pos.AnchorEnd(),
			Y = Pos.AnchorEnd(1),
			Text = "About",
			TextAlignment = Alignment.Center,
		};

		container.Add(filesTableView);
		container.Add(logTextView);

		this.Add(container);

		this.Add(concurrencyLabel);
		this.Add(concurrencyTextField);
		this.Add(subdirectoryLabel);
		this.Add(subdirectoryTextField);
		this.Add(extensionLabel);
		this.Add(extensionTextField);

		this.Add(ffmpegPathLabel);
		this.Add(ffmpegPathTextField);

		this.Add(ffprobePathLabel);
		this.Add(ffprobePathTextField);

		this.Add(argumentsLabel);
		this.Add(argumentsTextField);

		this.Add(startButton);
		this.Add(addFilesButton);
		this.Add(aboutButton);
	}
}