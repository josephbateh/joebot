namespace JoeBot.Abstractions;

public interface IEnvironment {
  string UserProfilePath { get; }
  int ProcessorCount { get; }
  void Exit(int exitCode);
}
