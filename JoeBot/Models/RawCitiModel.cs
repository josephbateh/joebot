namespace JoeBot.Models;

public struct RawCitiModel
{
  public string Status { get; set; }
  public string Date { get; set; }
  public string Description { get; set; }
  public string? Debit { get; set; }
  public string? Credit { get; set; }
}