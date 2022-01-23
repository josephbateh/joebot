using System.CommandLine;

namespace JoeBot.Commands.ProEnvironment
{
  public static class ParseCommand
  {
    public static Command Get()
    {
      var fileArgument = new Argument<string>("file", "File to be parsed.");
      var command = new Command("parse", "Parse a CSV from a banking institution.");
      command.AddArgument(fileArgument);
      command.SetHandler((string file) =>
      {
        Console.WriteLine($"Hello {file}!");
      }, fileArgument);
      return command;
    }
  }
}