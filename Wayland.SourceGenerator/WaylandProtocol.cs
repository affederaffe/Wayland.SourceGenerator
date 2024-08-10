using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false, ElementName = "protocol")]
    public class WaylandProtocol
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; } = null!;

        [DataMember(IsRequired = true)]
        [XmlElement(ElementName = "interface")]
        public WaylandInterface[] Interfaces { get; set; } = null!;
    }
}
