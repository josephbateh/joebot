using System.Xml.Serialization;

namespace JoeBot.Models.Rss;

[XmlRoot(ElementName = "item")]
public class Item {
  [XmlElement(ElementName = "title")]
  public List<string> Title { get; set; } = null!;

  [XmlElement(ElementName = "description")]
  public string Description { get; set; } = null!;

  [XmlElement(ElementName = "pubDate")]
  public string PubDate { get; set; } = null!;

  [XmlElement(ElementName = "author")]
  public List<string> Author { get; set; } = null!;

  [XmlElement(ElementName = "link")]
  public string Link { get; set; } = null!;

  [XmlElement(ElementName = "encoded")]
  public string Encoded { get; set; } = null!;

  [XmlElement(ElementName = "enclosure")]
  public Enclosure Enclosure { get; set; } = null!;

  [XmlElement(ElementName = "duration")]
  public DateTime Duration { get; set; }

  [XmlElement(ElementName = "summary")]
  public string Summary { get; set; } = null!;

  [XmlElement(ElementName = "subtitle")]
  public string Subtitle { get; set; } = null!;

  [XmlElement(ElementName = "explicit")]
  public string Explicit { get; set; } = null!;

  [XmlElement(ElementName = "episodeType")]
  public string EpisodeType { get; set; } = null!;
}