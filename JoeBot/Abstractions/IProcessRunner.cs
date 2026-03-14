namespace JoeBot.Abstractions;

public interface IProcessRunner {
  ProcessRunResult Run(
    string fileName,
    string arguments,
    string? workingDirectory = null,
    Action<string>? onStdoutLine = null,
    Action<string>? onStderrLine = null);
}

public record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
