using System.CommandLine;

namespace JoeBot.Commands;

public static class ExecuteCommand
{
  public static Command Get()
  {
    var command = new Command("execute", "Commands for executing things.");
    command.AddAlias("execute");
    command.AddCommand(ExecuteScriptCommand.Get());
    return command;
  }
}