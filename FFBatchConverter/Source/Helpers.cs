using System.Diagnostics;
using System.Text;

namespace FFBatchConverter;

public static class Helpers
{
	public static string? GetFFmpegPath()
	{
		return FindCommand("ffmpeg");
	}

	public static string? GetFFprobePath()
	{
		return FindCommand("ffprobe");
	}

	/// <summary>
	/// Given a file path, returns a list of all files in the directory and subdirectories.
	/// If it's a file, just returns itself in a list.
	/// An empty list is returned if the path does not exist.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static List<string> GetFilesRecursive(string path)
	{
		List<string> files = [];
		if (Directory.Exists(path))
		{
			foreach (string dir in Directory.GetFileSystemEntries(path))
			{
				files.AddRange(GetFilesRecursive(dir));
			}
		}

		if (File.Exists(path))
		{
			files.Add(path);
		}

		return files;
	}

	/// <summary>
	/// Runs ffprobe on the file and returns the json output as a string.
	/// </summary>
	/// <param name="ffprobePath"></param>
	/// <param name="filePath"></param>
	/// <returns></returns>
	public static string Probe(string ffprobePath, string filePath)
	{
		Process probe = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = ffprobePath,
				Arguments = $"-v quiet -print_format json -show_format \"{filePath}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			}
		};

		probe.Start();

		// Run ffprobe to get duration data
		StringBuilder probeOutput = new StringBuilder();
		while (!probe.StandardOutput.EndOfStream)
		{
			string? info = probe.StandardOutput.ReadLine();
			if (info != null)
				probeOutput.AppendLine(info);
		}

		return probeOutput.ToString();
	}

	/// <summary>
	/// Finds the path of the executable for the command.
	/// </summary>
	/// <returns>The first match for the command specified, or null if it could not be found.</returns>
	private static string? FindCommand(string command)
	{
		// Call the "where" or "which" command to find the path of the ffmpeg executable
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = OperatingSystem.IsWindows() ? "where" : "which",
			Arguments = command,
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		Process process = new Process
		{
			StartInfo = startInfo
		};

		process.Start();

		while (!process.StandardOutput.EndOfStream)
		{
			string? info = process.StandardOutput.ReadLine();
			if (info != null)
				return info;
		}

		return null;
	}
}