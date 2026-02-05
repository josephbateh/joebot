using JoeBot.Abstractions;

namespace JoeBot.Tests.Fakes;

public class FakeProcessRunner : IProcessRunner {
  private readonly Queue<ProcessRunResult> _results = new();
  public List<(string FileName, string Arguments, string? WorkingDirectory)> Calls { get; } = [];

  public void SetupNextResult(int exitCode, string stdout = "", string stderr = "") {
    _results.Enqueue(new ProcessRunResult(exitCode, stdout, stderr));
  }

  public ProcessRunResult Run(string fileName, string arguments, string? workingDirectory = null) {
    Calls.Add((fileName, arguments, workingDirectory));

    if (_results.Count == 0) {
      throw new InvalidOperationException(
          $"No result configured for process call. FileName: {fileName}, Arguments: {arguments}");
    }

    return _results.Dequeue();
  }
}
