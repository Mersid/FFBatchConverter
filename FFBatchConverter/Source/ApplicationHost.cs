using Terminal.Gui;

namespace FFBatchConverter;

public class ApplicationHost
{
    public static ApplicationHost Instance { get; } = new ApplicationHost();
    public BatchVideoEncoder Encoder { get; } = new BatchVideoEncoder();

    private ApplicationHost()
    {
    }

    public void Run()
    {
        Application.Init();

        try
        {
            Application.Run(new TopLevelView());
        }
        finally
        {
            Application.Shutdown();
        }
    }
}