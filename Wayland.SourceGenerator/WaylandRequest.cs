using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlRoot(ElementName = "request")]
    public class WaylandRequest : WaylandProtocolMessage
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; } = null!;
    }
}
