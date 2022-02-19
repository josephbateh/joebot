using System.CommandLine;
using JoeBot.Commands;

var root = new RootCommand
{
  Name = "joe",
  Description = "Joe Bot"
};

root.AddCommand(ParseCommand.Get());
root.AddCommand(GetCommand.Get());

return root.InvokeAsync(args).Result;