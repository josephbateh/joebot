using ConsoleTables;

namespace JoeBot.Helpers;

public class Table
{
  public Table(string[] columns)
  {
    Columns = columns;
    Rows = Array.Empty<string[]>();
  }

  private string[] Columns { get; }
  private string[][] Rows { get; set; }

  public void AddRow(string[] row)
  {
    Rows = Rows.Append(row).ToArray();
  }

  public void Print()
  {
    var table = new ConsoleTable(Columns);
    foreach (var row in Rows)
      // ReSharper disable once CoVariantArrayConversion
      table.AddRow(row);

    table.Write(Format.Minimal);
  }
}