using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace FFBatchConverter;

public class TopLevelView : Toplevel
{
	private new MenuBar MenuBar { get; } = new MenuBar();
	private FrameView Container { get; } = new FrameView();
	private View Child { get; } = new BatchVideoEncoderView();

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
		Container.Title = "FFBatchConverter";

		MenuBar.Menus =
		[
			new MenuBarItem("_File", [
				new MenuItem("_Quit", "", () => Application.RequestStop())
			]),
			new MenuBarItem("_Help", [
				new MenuItem("_About", "", () => MessageBox.Query("About", "FFBatchConverter\nBy Mersid\nhttps://github.com/Mersid/FFBatchConverter\nThis program is released in the hope that it will be useful.", "Continue"))
			])
		];

		Container.Add(Child);

		Add(MenuBar);
		Add(Container);
	}
}