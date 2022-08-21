using System.CommandLine;

namespace JoeBot.Commands;

public static class GetCommand
{
  public static Command Get()
  {
    var command = new Command("get", "Commands for getting things.");
    command.AddAlias("get");
    command.AddCommand(GetPodcastCommand.Get());
    command.AddCommand(GetInternetStatusCommand.Get());
    return command;
  }
}