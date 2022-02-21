using System.CommandLine;
using JoeBot.Commands;

var root = new RootCommand
{
  Name = "joe",
  Description = "JoeBot"
};

root.AddCommand(GetCommand.Get());

return root.InvokeAsync(args).Result;