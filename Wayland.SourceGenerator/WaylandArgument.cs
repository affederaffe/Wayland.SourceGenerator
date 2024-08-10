using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class WaylandArgument
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; } = null!;

        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; } = null!;

        [XmlAttribute(AttributeName = "interface")]
        public string? Interface { get; set; }

        [XmlAttribute(AttributeName = "summary")]
        public string? Summary { get; set; }

        [XmlAttribute(AttributeName = "enum")]
        public string? Enum { get; set; }

        [XmlAttribute(AttributeName = "allow-null")]
        public bool AllowNull { get; set; }
    }
}
