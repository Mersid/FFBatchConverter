using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace FFBatchConverter.Terminal;

public class TopLevelView : Toplevel
{
	private new MenuBar MenuBar { get; } = new MenuBar();
	private FrameView Container { get; } = new FrameView();
	private View BatchVideoEncoderView { get; } = new BatchVideoEncoderView(new BatchVideoEncoderViewModel());

	public TopLevelView()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		Width = Dim.Fill();
		Height = Dim.Fill();
		X = 0;
		Y = 0;
		Border.BorderStyle = LineStyle.Single;
		TextAlignment = Alignment.Start;
		Title = "Press Ctrl+Q to quit";
		ColorScheme = new ColorScheme(Colors.ColorSchemes["Base"])
		{
			Normal = new Attribute(Color.Cyan, Color.Black),
		};

		Container.X = 0;
		Container.Y = 1;
		Container.Width = Dim.Fill();
		Container.Height = Dim.Fill();
		Container.Title = "Batch Converter";

		MenuBar.Menus =
		[
			new MenuBarItem("_File", [
				new MenuItem("_Quit", "", () => Application.RequestStop())
			]),
			new MenuBarItem("_View", [
				new MenuItem("_Batch Video Encoder", "", () =>
				{
					Container.RemoveAll();
					Container.Add(BatchVideoEncoderView);
				})
			]),
			new MenuBarItem("_Help", [
				new MenuItem("_About", "", () => MessageBox.Query("About", "FFBatchConverter\nBy Mersid\nhttps://github.com/Mersid/FFBatchConverter\nThis program is released in the hope that it will be useful.", "Continue"))
			])
		];

		Container.Add(BatchVideoEncoderView);

		Add(MenuBar);
		Add(Container);
	}
}