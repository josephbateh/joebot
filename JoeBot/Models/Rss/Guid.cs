using System.Xml.Serialization;

[XmlRoot(ElementName="guid")]
public class Guid { 

	[XmlAttribute(AttributeName="isPermaLink")] 
	public bool IsPermaLink { get; set; } 

	[XmlText] 
	public string Text { get; set; } 
}
