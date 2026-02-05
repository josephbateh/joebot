using JoeBot.Abstractions;

namespace JoeBot.Adapters;

public class RealConsole : IConsole {
  public void WriteLine(string message) {
    Console.WriteLine(message);
  }

  public void WriteLine() {
    Console.WriteLine();
  }
}
