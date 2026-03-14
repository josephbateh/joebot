using JoeBot.Abstractions;

namespace JoeBot.Tests.Fakes;

public class FakeProcessRunner : IProcessRunner {
  private readonly Queue<ProcessRunResult> _results = new();
  public List<(string FileName, string Arguments, string? WorkingDirectory)> Calls { get; } = [];

  public void SetupNextResult(int exitCode, string stdout = "", string stderr = "") {
    _results.Enqueue(new ProcessRunResult(exitCode, stdout, stderr));
  }

  public ProcessRunResult Run(
    string fileName,
    string arguments,
    string? workingDirectory = null,
    Action<string>? onStdoutLine = null,
    Action<string>? onStderrLine = null) {
    Calls.Add((fileName, arguments, workingDirectory));

    if (_results.Count == 0) {
      throw new InvalidOperationException(
          $"No result configured for process call. FileName: {fileName}, Arguments: {arguments}");
    }

    var result = _results.Dequeue();

    if (onStdoutLine != null && !string.IsNullOrEmpty(result.StandardOutput)) {
      foreach (var line in result.StandardOutput.Split(["\r\n", "\n"], StringSplitOptions.None)) {
        onStdoutLine(line);
      }
    }

    if (onStderrLine != null && !string.IsNullOrEmpty(result.StandardError)) {
      foreach (var line in result.StandardError.Split(["\r\n", "\n"], StringSplitOptions.None)) {
        onStderrLine(line);
      }
    }

    return result;
  }
}
