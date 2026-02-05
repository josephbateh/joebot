using System.CommandLine;

namespace JoeBot.Commands.Rename;

public class RenameCommand
{
    public static Command Get()
    {
        var command = new Command("rename", "Commands for renaming things.");
        command.Subcommands.Add(RenameFileCommand.Get());
        return command;
    }
}
