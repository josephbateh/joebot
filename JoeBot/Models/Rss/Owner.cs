using System.Xml.Serialization;

[XmlRoot(ElementName="owner")]
public class Owner { 

	[XmlElement(ElementName="name")] 
	public string Name { get; set; } 

	[XmlElement(ElementName="email")] 
	public string Email { get; set; } 
}
