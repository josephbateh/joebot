using System.CommandLine;
using JoeBot.Commands.Convert;

namespace JoeBot.Commands;

public static class ConvertCommand
{
    public static Command Get()
    {
        var command = new Command("convert", "Commands for converting files.");
        command.AddCommand(ConvertVideoCommand.Get());
        return command;
    }
}
