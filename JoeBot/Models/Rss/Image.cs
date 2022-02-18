using System.Xml.Serialization;

[XmlRoot(ElementName="image")]
public class Image { 

	[XmlElement(ElementName="link")] 
	public string Link { get; set; } 

	[XmlElement(ElementName="title")] 
	public string Title { get; set; } 

	[XmlElement(ElementName="url")] 
	public string Url { get; set; } 

	[XmlAttribute(AttributeName="href")] 
	public string Href { get; set; } 
}
