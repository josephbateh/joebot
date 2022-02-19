using System.CommandLine;

namespace JoeBot.Commands;

public static class ParseCommand
{
  public static Command Get()
  {
    var command = new Command("parse", "Commands to parse files.");
    command.AddCommand(ParseCsvCommand.Get());
    return command;
  }
}