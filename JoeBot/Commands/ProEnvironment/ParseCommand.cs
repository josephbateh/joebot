using System.CommandLine;
using JoeBot.Helpers;

namespace JoeBot.Commands.ProEnvironment
{
  public static class ParseCommand
  {
    public static Command Get()
    {
      var fileArgument = new Argument<string>("file", "File to be parsed.");
      var command = new Command("parse", "Parse a CSV from a banking institution.");
      command.AddArgument(fileArgument);
      command.SetHandler((string file) =>
      {
        var table = new Table(new[]
        {
          "Name",
          "Date",
          "Value",
          "File"
        });
        
        table.AddRow(new []
        {
          "Publix",
          "2022-01-23",
          "$420.69",
          file
        });
        
        table.Print();
      }, fileArgument);
      return command;
    }
  }
}