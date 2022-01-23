// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using JoeBot.Commands.ProEnvironment;

var root = new RootCommand
{
    Name = "joe",
    Description = "Joe Bot"
};

root.AddCommand(BankingCommand.Get());

return root.InvokeAsync(args).Result;