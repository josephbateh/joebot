using System.Diagnostics;
using JoeBot.Abstractions;

namespace JoeBot.Adapters;

public class RealProcessRunner : IProcessRunner {
  public ProcessRunResult Run(string fileName, string arguments, string? workingDirectory = null) {
    var processStartInfo = new ProcessStartInfo {
      FileName = fileName,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    if (workingDirectory != null) {
      processStartInfo.WorkingDirectory = workingDirectory;
    }

    using var process = new Process();
    process.StartInfo = processStartInfo;

    var stdout = new System.Text.StringBuilder();
    var stderr = new System.Text.StringBuilder();

    process.OutputDataReceived += (_, e) => {
      if (e.Data != null) {
        stdout.AppendLine(e.Data);
      }
    };

    process.ErrorDataReceived += (_, e) => {
      if (e.Data != null) {
        stderr.AppendLine(e.Data);
      }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    return new ProcessRunResult(process.ExitCode, stdout.ToString(), stderr.ToString());
  }
}
