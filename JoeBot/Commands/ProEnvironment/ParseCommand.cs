using System.CommandLine;
using System.Globalization;
using CsvHelper;
using JoeBot.Helpers;
using JoeBot.Models;

namespace JoeBot.Commands.ProEnvironment
{
  public static class ParseCommand
  {
    public static Command Get()
    {
      var fileArgument = new Argument<string>("file", "File to be parsed.");
      var command = new Command("parse", "Parse a CSV from a banking institution.");
      command.AddArgument(fileArgument);
      command.SetHandler((string path) =>
      {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        var records = csv.GetRecords<RawCitiModel>();
        
        var table = new Table(new[]
        {
          "Date",
          "Description",
          "Value"
        });

        foreach (var record in records)
        {
          var value = record.Credit!.Equals("") ? record.Debit : record.Credit;
          table.AddRow(new []
          {
            record.Date,
            record.Description,
            value!.ToString(CultureInfo.InvariantCulture)
          });
        }
        
        table.Print();
      }, fileArgument);
      return command;
    }
  }
}