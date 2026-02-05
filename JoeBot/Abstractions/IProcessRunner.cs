namespace JoeBot.Abstractions;

public interface IProcessRunner {
  ProcessRunResult Run(string fileName, string arguments, string? workingDirectory = null);
}

public record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
