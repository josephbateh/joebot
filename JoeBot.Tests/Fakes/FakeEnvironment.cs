using JoeBot.Abstractions;

namespace JoeBot.Tests.Fakes;

public class FakeEnvironment : IEnvironment {
  public string UserProfilePath { get; set; } = "/home/testuser";
  public int ProcessorCount { get; set; } = 4;
  public int? ExitCode { get; private set; }

  public void Exit(int exitCode) {
    ExitCode = exitCode;
  }

  public void Reset() {
    ExitCode = null;
  }
}
