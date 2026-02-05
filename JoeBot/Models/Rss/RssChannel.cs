using System.Xml.Serialization;

namespace JoeBot.Models.Rss;

[XmlRoot(ElementName = "channel")]
public class RssChannel {
  [XmlElement(ElementName = "generator")]
  public string Generator { get; set; } = null!;

  [XmlElement(ElementName = "title")]
  public string Title { get; set; } = null!;

  [XmlElement(ElementName = "description")]
  public string Description { get; set; } = null!;

  [XmlElement(ElementName = "copyright")]
  public string Copyright { get; set; } = null!;

  [XmlElement(ElementName = "language")]
  public string Language { get; set; } = null!;

  [XmlElement(ElementName = "pubDate")]
  public string PubDate { get; set; } = null!;

  [XmlElement(ElementName = "lastBuildDate")]
  public string LastBuildDate { get; set; } = null!;

  [XmlElement(ElementName = "type")]
  public string Type { get; set; } = null!;

  [XmlElement(ElementName = "summary")]
  public string Summary { get; set; } = null!;

  [XmlElement(ElementName = "author")]
  public string Author { get; set; } = null!;

  [XmlElement(ElementName = "explicit")]
  public string Explicit { get; set; } = null!;

  [XmlElement(ElementName = "new-feed-url")]
  public string Newfeedurl { get; set; } = null!;

  [XmlElement(ElementName = "item")]
  public List<Item> Item { get; set; } = null!;
}