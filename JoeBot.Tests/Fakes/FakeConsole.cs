using JoeBot.Abstractions;

namespace JoeBot.Tests.Fakes;

public class FakeConsole : IConsole {
  public List<string> Lines { get; } = [];

  public void WriteLine(string message) {
    Lines.Add(message);
  }

  public void WriteLine() {
    Lines.Add(string.Empty);
  }

  public string GetAllOutput() => string.Join(System.Environment.NewLine, Lines);
}
