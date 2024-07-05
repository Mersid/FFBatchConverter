using ReactiveUI;
using Terminal.Gui;

namespace FFBatchConverter;

public sealed class BatchVideoEncoderView : View
{
    private View Container { get; }
    private TableView FilesTableView { get; }

    private TextView LogTextView { get; }

    private Label ConcurrencyLabel { get; }
    private TextField ConcurrencyTextField { get; }

    private Label SubdirectoryLabel { get; }
    private TextField SubdirectoryTextField { get; }

    private Label ExtensionLabel { get; }
    private TextField ExtensionTextField { get; }

    private Label FfmpegPathLabel { get; }
    private TextField FfmpegPathTextField { get; }

    private Label FfprobePathLabel { get; }
    private TextField FfprobePathTextField { get; }

    private Label ArgumentsLabel { get; }
    private TextField ArgumentsTextField { get; }

    private Button StartButton { get; }
    private Button AddFilesButton { get; }

    public BatchVideoEncoderView(BatchVideoEncoderViewModel model)
    {
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Holds the list and text box
		Container = new View
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 4,
		};

		model.FilesDataTable.Columns.Add("File name", typeof(string));
		model.FilesDataTable.Columns.Add("Duration", typeof(string));
		model.FilesDataTable.Columns.Add("Size", typeof(string));
		model.FilesDataTable.Columns.Add("Status", typeof(string));

		FilesTableView = new TableView
		{
			Width = Dim.Fill(),
			Height = Dim.Percent(50),
			X = 0,
			Y = 0,
			FullRowSelect = true,
			MultiSelect = false,
			Table = new DataTableSource(model.FilesDataTable),
		};
		model
			.WhenAnyValue(x => x.Reactor)
			.Subscribe(x =>
			{
				FilesTableView.Update();
			});
		FilesTableView.SelectedCellChanged += (sender, args) =>
		{
			model.SelectedRow = args.NewRow;
		};

		LogTextView = new TextView
		{
			X = 0,
			Y = Pos.Bottom(FilesTableView),
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			Text = "",
			ReadOnly = true
		};
		model
			.WhenAnyValue(x => x.Log)
			.Subscribe(x =>
			{
				LogTextView.Selecting = false;
				LogTextView.Text = x;

				// Otherwise, it'll jump to top...
				LogTextView.MoveEnd();
			});

		ConcurrencyLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(4),
			Text = "Concurrency",
		};

		ConcurrencyTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(4),
			Width = 3,
			Height = 1,
		};
		model
			.WhenAnyValue(x => x.Concurrency)
			.BindTo(ConcurrencyTextField, x => x.Text);
		ConcurrencyTextField.TextChanged += (sender, args) => model.Concurrency = ConcurrencyTextField.Text;

		SubdirectoryLabel = new Label
		{
			X = 20,
			Y = Pos.AnchorEnd(4),
			Text = "Subdir",
		};

		SubdirectoryTextField = new TextField
		{
			X = 28,
			Y = Pos.AnchorEnd(4),
			Width = 30,
			Height = 1,
		};
		model
			.WhenAnyValue(x => x.Subdirectory)
			.BindTo(SubdirectoryTextField, x => x.Text);
		SubdirectoryTextField.TextChanged += (sender, args) => model.Subdirectory = SubdirectoryTextField.Text;

		ExtensionLabel = new Label
		{
			X = 61,
			Y = Pos.AnchorEnd(4),
			Text = "Extension",
		};

		ExtensionTextField = new TextField
		{
			X = 72,
			Y = Pos.AnchorEnd(4),
			Width = 6,
			Height = 1,
		};
		model
			.WhenAnyValue(x => x.Extension)
			.BindTo(ExtensionTextField, x => x.Text);
		ExtensionTextField.TextChanged += (sender, args) => model.Extension = ExtensionTextField.Text;

		FfmpegPathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(3),
			Text = "FFmpeg Path",
		};

		FfmpegPathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(3),
			Width = 64,
			Height = 1,
		};
		model
			.WhenAnyValue(x => x.FfmpegPath)
			.BindTo(FfmpegPathTextField, x => x.Text);
		FfmpegPathTextField.TextChanged += (sender, args) => model.FfmpegPath = FfmpegPathTextField.Text;

		FfprobePathLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(2),
			Text = "FFprobe Path",
		};

		FfprobePathTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(2),
			Width = 64,
			Height = 1,
		};
		model
			.WhenAnyValue(x => x.FfprobePath)
			.BindTo(FfprobePathTextField, x => x.Text);
		FfprobePathTextField.TextChanged += (sender, args) => model.FfprobePath = FfprobePathTextField.Text;

		ArgumentsLabel = new Label
		{
			X = 0,
			Y = Pos.AnchorEnd(1),
			Text = "Arguments",
		};

		ArgumentsTextField = new TextField
		{
			X = 14,
			Y = Pos.AnchorEnd(1),
			Width = 64,
			Height = 1,
		};
		model
			.WhenAnyValue(x => x.Arguments)
			.BindTo(ArgumentsTextField, x => x.Text);
		ArgumentsTextField.TextChanged += (sender, args) => model.Arguments = ArgumentsTextField.Text;

		StartButton = new Button
		{
			X = Pos.AnchorEnd(),
			Y = Pos.AnchorEnd(2),
			TextAlignment = Alignment.Center,
		};
		model
			.WhenAnyValue(x => x.Encoding)
			.Subscribe(s => // s is the new value. So if the state is false and we hit the button, s is passed as true.
			{
				StartButton.Text = s ? "Stop" : "Start";
			});
		StartButton.Accept += model.StartButtonPressed;

		AddFilesButton = new Button
		{
			X = Pos.AnchorEnd(),
			Y = Pos.AnchorEnd(1),
			Text = "Add files",
			TextAlignment = Alignment.Center,
		};
		AddFilesButton.Accept += model.AddFilesButtonPressed;

		Container.Add(FilesTableView);
		Container.Add(LogTextView);

		Add(Container);

		Add(ConcurrencyLabel);
		Add(ConcurrencyTextField);
		Add(SubdirectoryLabel);
		Add(SubdirectoryTextField);
		Add(ExtensionLabel);
		Add(ExtensionTextField);

		Add(FfmpegPathLabel);
		Add(FfmpegPathTextField);

		Add(FfprobePathLabel);
		Add(FfprobePathTextField);

		Add(ArgumentsLabel);
		Add(ArgumentsTextField);

		Add(StartButton);
		Add(AddFilesButton);
    }
}