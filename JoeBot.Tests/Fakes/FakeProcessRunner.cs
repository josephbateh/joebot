using JoeBot.Abstractions;

namespace JoeBot.Tests.Fakes;

public class FakeProcessRunner : IProcessRunner {
  private readonly object _lock = new();
  private readonly Queue<ProcessRunResult> _results = new();
  private readonly List<(Func<string, string, bool> Match, ProcessRunResult Result)> _keyedResults = [];
  private readonly List<(string FileName, string Arguments, string? WorkingDirectory)> _calls = [];

  // Bulk/directory mode runs conversions concurrently via Task.Run with no
  // synchronization, so this can be read and written from multiple threads at
  // once. Snapshot it under the lock rather than exposing the backing list
  // directly, since List<T> isn't thread-safe for concurrent Add + enumerate.
  public List<(string FileName, string Arguments, string? WorkingDirectory)> Calls {
    get { lock (_lock) { return [.. _calls]; } }
  }

  public void SetupNextResult(int exitCode, string stdout = "", string stderr = "") {
    _results.Enqueue(new ProcessRunResult(exitCode, stdout, stderr));
  }

  // For tests where call order isn't deterministic (e.g. genuinely parallel
  // work), pin a result to whichever call matches a predicate on fileName and
  // arguments instead of relying on FIFO order. Checked before the queue.
  public void SetupResultFor(Func<string, string, bool> match, int exitCode, string stdout = "", string stderr = "") {
    _keyedResults.Add((match, new ProcessRunResult(exitCode, stdout, stderr)));
  }

  public ProcessRunResult Run(
    string fileName,
    string arguments,
    string? workingDirectory = null,
    Action<string>? onStdoutLine = null,
    Action<string>? onStderrLine = null) {
    ProcessRunResult result;
    lock (_lock) {
      _calls.Add((fileName, arguments, workingDirectory));

      var keyedMatch = _keyedResults.FirstOrDefault(k => k.Match(fileName, arguments));
      if (keyedMatch.Match != null) {
        result = keyedMatch.Result;
      }
      else {
        if (_results.Count == 0) {
          throw new InvalidOperationException(
              $"No result configured for process call. FileName: {fileName}, Arguments: {arguments}");
        }

        result = _results.Dequeue();
      }
    }

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
