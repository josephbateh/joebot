using System.CommandLine;

namespace JoeBot.Commands;

public static class GetCommand {
  public static Command Get() {
    var command = new Command("get", "Commands for getting things.");
    command.Subcommands.Add(GetPodcastCommand.Get());
    command.Subcommands.Add(GetInternetStatusCommand.Get());
    return command;
  }
}