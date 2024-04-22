using System.Data;

namespace FFBatchConverter;

using System;
using Terminal.Gui;


public partial class MyView : Window
{
	private TableView filesTableView;

	private Label ffmpegPathLabel;
	private TextField ffmpegPathTextField;

	private Label ffprobePathLabel;
	private TextField ffprobePathTextField;

	private Label commandLabel;
	private TextField commandTextField;

	private Label concurrencyLabel;
	private TextField concurrencyTextField;

	private Label extensionLabel;
	private TextField extensionTextField;

	private Label subdirectoryLabel;
	private TextField subdirectoryTextField;

	private Button startButton;
	private Button addFilesButton;

	private TextView logTextView;
	private View color;

	private void InitializeComponent()
	{
		this.color = new View
		{
			X = 0,
			Y = 30,
			Width = Dim.Fill(),
			Height = 1,
			ColorScheme = new ColorScheme
			{
				Normal = Application.Driver.MakeAttribute(Color.White, Color.BrightCyan),
			}
		};

		this.logTextView = new TextView
		{
			X = 0,
			Y = 23,
			Width = Dim.Fill(),
			Height = 5,
			Text = "Hello, world!",
		};

		this.subdirectoryLabel = new Label
		{
			X = 20,
			Y = Pos.AnchorEnd(4),
			Width = 10,
			Height = 1,
			Data = "label5",
			Text = "Subdir",
			TextAlignment = TextAlignment.Left,
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
			Width = 6,
			Height = 1,
			Data = "label4",
			Text = "Extension",
			TextAlignment = TextAlignment.Left,
		};

		this.extensionTextField = new TextField
		{
			X = 72,
			Y = Pos.AnchorEnd(4),
			Width = 6,
			Height = 1,
			Text = "mkv",
		};

		this.concurrencyLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(4),
			Width = 4,
			Height = 1,
			Data = "label3",
			Text = "Concurrency",
			TextAlignment = TextAlignment.Left,
		};

		this.concurrencyTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(4),
			Width = 3,
			Height = 1,
			Text = "1",
		};

		this.addFilesButton = new Button
		{
			Y = Pos.AnchorEnd(1),
			Data = "button1",
			Text = "Add files",
			TextAlignment = TextAlignment.Centered,
			IsDefault = false
		};

		this.startButton = new Button
		{
			Y = Pos.AnchorEnd(2),
			Data = "button2",
			Text = "Start",
			TextAlignment = TextAlignment.Centered,
			IsDefault = false
		};

		// Right-align buttons
		this.addFilesButton.X = Pos.AnchorEnd() - (Pos.Right(addFilesButton) - Pos.Left(addFilesButton));
		this.startButton.X = Pos.AnchorEnd() - (Pos.Right(startButton) - Pos.Left(startButton));

		this.ffmpegPathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(3),
			Width = 4,
			Height = 1,
			Data = "label1",
			Text = "FFmpeg Path",
			TextAlignment = TextAlignment.Left,
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
			Width = 4,
			Height = 1,
			Data = "label1",
			Text = "FFprobe Path",
			TextAlignment = TextAlignment.Left,
		};

		this.ffprobePathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(2),
			Width = 64,
			Text = Helpers.GetFFprobePath() ?? "",
			Height = 1,
		};

		FilesDataTable = new DataTable();
		FilesDataTable.Columns.Add("File name", typeof(string));
		FilesDataTable.Columns.Add("Duration", typeof(string));
		FilesDataTable.Columns.Add("Size", typeof(string));
		FilesDataTable.Columns.Add("Status", typeof(string));

		this.filesTableView = new TableView
		{
			Width = Dim.Fill(),
			Height = 20,
			X = 0,
			Y = 0,
			FullRowSelect = true,
			MultiSelect = true,
			Table = FilesDataTable,
			ColorScheme = new ColorScheme
			{
				Normal = Application.Driver.MakeAttribute(Color.Cyan, Color.Green),
			}
		};

		this.commandLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(1),
			Width = 4,
			Height = 1,
			Data = "label2",
			Text = "Arguments",
			TextAlignment = TextAlignment.Left,
		};

		this.commandTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(1),
			Width = 64,
			Height = 1,
			Text = "-c:v libx265 -c:a aac"
		};

		this.Width = Dim.Fill(0);
		this.Height = Dim.Fill(0);
		this.X = 0;
		this.Y = 0;
		this.Modal = false;
		this.Text = "";
		this.Border.BorderStyle = BorderStyle.Single;
		this.Border.Effect3D = false;
		this.Border.DrawMarginFrame = true;
		this.TextAlignment = TextAlignment.Left;
		this.Title = "Press Ctrl+Q to quit";
		this.ColorScheme.Normal = Application.Driver.MakeAttribute(Color.Green, Color.Black);

		this.Add(filesTableView);

		this.Add(ffmpegPathLabel);
		this.Add(ffmpegPathTextField);

		this.Add(ffprobePathLabel);
		this.Add(ffprobePathTextField);

		this.Add(concurrencyLabel);
		this.Add(concurrencyTextField);

		this.Add(extensionLabel);
		this.Add(extensionTextField);

		this.Add(subdirectoryLabel);
		this.Add(subdirectoryTextField);

		this.Add(commandLabel);
		this.Add(commandTextField);

		this.Add(startButton);
		this.Add(addFilesButton);

		this.Add(logTextView);
		this.Add(color);
	}
}