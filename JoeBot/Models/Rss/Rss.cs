using System.Xml.Serialization;

[XmlRoot(ElementName="rss")]
public class Rss { 

	[XmlElement(ElementName="channel")] 
	public Channel Channel { get; set; } 

	[XmlText] 
	public string Text { get; set; } 
}
