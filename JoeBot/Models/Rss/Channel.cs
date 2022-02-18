using System.Xml.Serialization;

[XmlRoot(ElementName="channel")]
public class Channel { 

	[XmlElement(ElementName="link")] 
	public List<Link> Link { get; set; } 

	[XmlElement(ElementName="generator")] 
	public string Generator { get; set; } 

	[XmlElement(ElementName="title")] 
	public string Title { get; set; } 

	[XmlElement(ElementName="description")] 
	public string Description { get; set; } 

	[XmlElement(ElementName="copyright")] 
	public string Copyright { get; set; } 

	[XmlElement(ElementName="language")] 
	public string Language { get; set; } 

	[XmlElement(ElementName="pubDate")] 
	public string PubDate { get; set; } 

	[XmlElement(ElementName="lastBuildDate")] 
	public string LastBuildDate { get; set; } 

	[XmlElement(ElementName="image")] 
	public List<Image> Image { get; set; } 

	[XmlElement(ElementName="type")] 
	public string Type { get; set; } 

	[XmlElement(ElementName="summary")] 
	public string Summary { get; set; } 

	[XmlElement(ElementName="author")] 
	public string Author { get; set; } 

	[XmlElement(ElementName="explicit")] 
	public string Explicit { get; set; } 

	[XmlElement(ElementName="new-feed-url")] 
	public string Newfeedurl { get; set; } 

	[XmlElement(ElementName="owner")] 
	public Owner Owner { get; set; } 

	[XmlElement(ElementName="item")] 
	public List<Item> Item { get; set; } 
}
