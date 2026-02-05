using System.Xml.Serialization;

namespace JoeBot.Models.Rss;

[XmlRoot(ElementName = "rss")]
public class RssFeed {
  [XmlElement(ElementName = "channel")]
  public RssChannel Channel { get; set; } = null!;
}