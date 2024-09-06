using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FFBatchConverter.Avalonia;

public class LogHelper
{
    private static readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "FFBatchConverter");

    public static void OpenLog(string log)
    {
        if (!Directory.Exists(_tempDirectory))
            Directory.CreateDirectory(_tempDirectory);

        string tempFile = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}.log");
        File.WriteAllText(tempFile, log);
        Process.Start(new ProcessStartInfo(tempFile) {UseShellExecute = true});
    }
}