using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FFBatchConverter.Avalonia.Views;

public partial class AboutView : Window
{
    public AboutView()
    {
        InitializeComponent();
    }

    public void Close(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}