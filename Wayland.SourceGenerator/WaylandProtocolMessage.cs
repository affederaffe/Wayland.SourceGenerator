using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    public abstract class WaylandProtocolMessage
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; } = null!;

        [XmlAttribute(AttributeName = "since")]
        public int Since { get; set; }

        [XmlAttribute(AttributeName = "deprecated-since")]
        public int DeprecatedSince { get; set; }

        [XmlElement(ElementName = "description")]
        public WaylandDescription? Description { get; set; }

        [XmlElement(ElementName = "arg")]
        public WaylandArgument[]? Arguments { get; set; }
    }
}
