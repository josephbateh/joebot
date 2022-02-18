using System.Xml.Serialization;

[XmlRoot(ElementName="link")]
public class Link { 

	[XmlAttribute(AttributeName="href")] 
	public string Href { get; set; } 

	[XmlAttribute(AttributeName="rel")] 
	public string Rel { get; set; } 

	[XmlAttribute(AttributeName="title")] 
	public string Title { get; set; } 

	[XmlAttribute(AttributeName="type")] 
	public string Type { get; set; } 

	[XmlAttribute(AttributeName="xmlns")] 
	public string Xmlns { get; set; } 
}
