using System.Xml.Serialization;

[XmlRoot(ElementName="enclosure")]
public class Enclosure { 

	[XmlAttribute(AttributeName="length")] 
	public int Length { get; set; } 

	[XmlAttribute(AttributeName="type")] 
	public string Type { get; set; } 

	[XmlAttribute(AttributeName="url")] 
	public string Url { get; set; } 
}
