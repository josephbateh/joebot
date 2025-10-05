using System.CommandLine;
using JoeBot.Commands;
using JoeBot.Commands.Rename;

var root = new RootCommand
{
  Name = "joe",
  Description = "JoeBot"
};

root.AddCommand(GetCommand.Get());
root.AddCommand(ExecuteCommand.Get());
root.AddCommand(RenameCommand.Get());

return root.InvokeAsync(args).Result;