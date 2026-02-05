using System.CommandLine;

namespace JoeBot.Commands;

public static class ExecuteCommand {
  public static Command Get() {
    var command = new Command("execute", "Commands for executing things.");
    command.Subcommands.Add(ExecuteScriptCommand.Get());
    return command;
  }
}