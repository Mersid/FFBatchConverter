using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FFBatchConverter.Avalonia.ViewModels;
using FFBatchConverter.Avalonia.Views;

namespace FFBatchConverter.Avalonia;

public partial class App : Application
{
    public static App Instance { get; private set; } = null!;

    public BatchVideoEncoder Encoder { get; } = new BatchVideoEncoder();

    public override void Initialize()
    {
        Instance = this;
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}