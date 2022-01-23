using System.CommandLine;

namespace JoeBot.Commands.ProEnvironment;

public static class BankingCommand
{
  public static Command Get()
  {
    var command = new Command("banking", "Commands banking tasks.");
    command.AddAlias("bank");
    command.AddCommand(ParseCommand.Get());
    return command;
  }
}