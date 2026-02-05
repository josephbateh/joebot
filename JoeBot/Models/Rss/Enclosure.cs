using System.Xml.Serialization;

namespace JoeBot.Models.Rss;

[XmlRoot(ElementName = "enclosure")]
public class Enclosure {
  [XmlAttribute(AttributeName = "length")]
  public int Length { get; set; }

  [XmlAttribute(AttributeName = "type")]
  public string Type { get; set; } = null!;

  [XmlAttribute(AttributeName = "url")]
  public string Url { get; set; } = null!;
}