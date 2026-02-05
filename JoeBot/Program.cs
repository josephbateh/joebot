using System.CommandLine;
using JoeBot.Commands;
using JoeBot.Commands.Rename;

var root = new RootCommand("JoeBot");

root.Subcommands.Add(GetCommand.Get());
root.Subcommands.Add(ExecuteCommand.Get());
root.Subcommands.Add(RenameCommand.Get());
root.Subcommands.Add(ConvertCommand.Get());

return root.Parse(args).Invoke();