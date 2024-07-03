using System.Reactive.Linq;
using ReactiveUI;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace FFBatchConverter;

public sealed class MirrorView : View
{
    private TextField TextField { get; } = new TextField();
    private Label Label { get; } = new Label();
    private MirrorViewModel ViewModel { get; }
    public MirrorView(MirrorViewModel model)
    {
        ViewModel = model;
        Width = Dim.Fill();
        Height = Dim.Fill();

        TextField.X = 0;
        TextField.Y = 0;
        TextField.Width = 40;

        Label.X = 0;
        Label.Y = 1;
        Label.Width = 40;
        Label.ColorScheme = new ColorScheme(Colors.ColorSchemes["Base"])
        {
            Normal = new Attribute(Colors.ColorSchemes["Base"]!.Normal.Foreground, ColorName.Green)
        };

        model
            .WhenAnyValue(x => x.Text)
            .BindTo(TextField, x => x.Text);

        model
            .WhenAnyValue(x => x.Text)
            .Select(text => $"Text: {text}")
            .BindTo(Label, label => label.Text);

        // model.PropertyChanged += (sender, args) =>
        // {
        //     if (args.PropertyName == nameof(model.Text))
        //     {
        //         TextField.Text = model.Text;
        //     }
        // };

        // TextField
        //     .Events()
        //     .TextChanged
        //     .Select(_ => TextField.Text)
        //     .DistinctUntilChanged()
        //     .BindTo(model, x => x.Text);

        TextField.TextChanged += (sender, args) => model.Text = TextField.Text;

        Add(TextField);
        Add(Label);
    }
}