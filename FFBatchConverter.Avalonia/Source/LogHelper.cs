using System;
using System.Diagnostics;
using System.IO;

namespace FFBatchConverter.Avalonia;

public static class LogHelper
{
    private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "FFBatchConverter");

    public static void OpenLog(string log)
    {
        if (!Directory.Exists(TempDirectory))
            Directory.CreateDirectory(TempDirectory);

        string tempFile = Path.Combine(TempDirectory, $"{Guid.NewGuid()}.log");
        File.WriteAllText(tempFile, log);
        Process.Start(new ProcessStartInfo(tempFile) {UseShellExecute = true});
    }
}