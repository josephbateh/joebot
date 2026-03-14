using JoeBot.Abstractions;

namespace JoeBot.Adapters;

public class RealEnvironment : IEnvironment {
  public string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

  public int ProcessorCount => Environment.ProcessorCount;

  public void Exit(int exitCode) {
    Environment.Exit(exitCode);
  }
}
