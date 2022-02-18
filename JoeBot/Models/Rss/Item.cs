using System.Xml.Serialization;

[XmlRoot(ElementName="item")]
public class Item { 

	[XmlElement(ElementName="title")] 
	public List<string> Title { get; set; } 

	[XmlElement(ElementName="description")] 
	public string Description { get; set; } 

	[XmlElement(ElementName="pubDate")] 
	public string PubDate { get; set; } 

	[XmlElement(ElementName="author")] 
	public List<string> Author { get; set; } 

	[XmlElement(ElementName="link")] 
	public string Link { get; set; } 

	[XmlElement(ElementName="encoded")] 
	public string Encoded { get; set; } 

	[XmlElement(ElementName="enclosure")] 
	public Enclosure Enclosure { get; set; } 

	[XmlElement(ElementName="image")] 
	public Image Image { get; set; } 

	[XmlElement(ElementName="duration")] 
	public DateTime Duration { get; set; } 

	[XmlElement(ElementName="summary")] 
	public string Summary { get; set; } 

	[XmlElement(ElementName="subtitle")] 
	public string Subtitle { get; set; } 

	[XmlElement(ElementName="explicit")] 
	public string Explicit { get; set; } 

	[XmlElement(ElementName="episodeType")] 
	public string EpisodeType { get; set; } 
}
