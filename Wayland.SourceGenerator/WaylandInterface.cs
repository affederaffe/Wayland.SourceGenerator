using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class WaylandInterface
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; } = null!;

        [XmlAttribute(AttributeName = "version")]
        public int Version { get; set; }

        [XmlElement(ElementName = "description")]
        public WaylandDescription? Description { get; set; }

        [XmlElement(ElementName = "enum")]
        public WaylandEnum[]? Enums { get; set; }

        [XmlElement(ElementName = "event")]
        public WaylandEvent[]? Events { get; set; }

        [XmlElement(ElementName = "request")]
        public WaylandRequest[]? Requests { get; set; }
    }
}
