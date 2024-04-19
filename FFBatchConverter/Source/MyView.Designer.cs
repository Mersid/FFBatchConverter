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

	private Button startButton;
	private Button addFilesButton;

	private void InitializeComponent()
	{
		this.extensionLabel = new Label
		{
			X = 20,
			Y = Pos.AnchorEnd(2),
			Width = 6,
			Height = 1,
			Data = "label4",
			Text = "Extension",
			TextAlignment = TextAlignment.Left,
		};

		this.extensionTextField = new TextField
		{
			X = 32,
			Y = Pos.AnchorEnd(2),
			Width = 6,
			Height = 1,
			Text = "mkv",
		};

		this.concurrencyLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(2),
			Width = 4,
			Height = 1,
			Data = "label3",
			Text = "Concurrency",
			TextAlignment = TextAlignment.Left,
		};

		this.concurrencyTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(2),
			Width = 3,
			Height = 1,
			Text = "1",
		};

		this.addFilesButton = new Button
		{
			Y = Pos.AnchorEnd(1),
			Width = 12,
			Data = "button1",
			Text = "Add files",
			TextAlignment = TextAlignment.Centered,
			IsDefault = false
		};

		this.startButton = new Button
		{
			Y = Pos.AnchorEnd(2),
			Width = 12,
			Data = "button2",
			Text = "Start",
			TextAlignment = TextAlignment.Centered,
			IsDefault = false
		};

		this.addFilesButton.X = Pos.AnchorEnd(addFilesButton.Text.Length + 4);
		this.startButton.X = Pos.AnchorEnd(startButton.Text.Length + 6);

		this.ffmpegPathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(4),
			Width = 4,
			Height = 1,
			Data = "label1",
			Text = "FFmpeg Path",
			TextAlignment = TextAlignment.Left,
		};

		this.ffmpegPathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(4),
			Width = 64,
			Text = Helpers.GetFFmpegPath() ?? "",
			Height = 1,
		};

		this.ffprobePathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(3),
			Width = 4,
			Height = 1,
			Data = "label1",
			Text = "FFprobe Path",
			TextAlignment = TextAlignment.Left,
		};

		this.ffprobePathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(3),
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
		};

		this.commandLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(1),
			Width = 4,
			Height = 1,
			Data = "label2",
			Text = "FFmpeg Command",
			TextAlignment = TextAlignment.Left,
		};

		this.commandTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(1),
			Width = 64,
			Height = 1,
			Enabled = false,
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

		this.Add(commandLabel);
		this.Add(commandTextField);

		this.Add(startButton);
		this.Add(addFilesButton);

	}
}