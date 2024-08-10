using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class WaylandEnumEntry
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; } = null!;

        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; } = null!;

        [XmlAttribute(AttributeName = "summary")]
        public string? Summary { get; set; }

        [XmlAttribute(AttributeName = "since")]
        public int Since { get; set; }

        [XmlAttribute(AttributeName = "deprecated-since")]
        public int DeprecatedSince { get; set; }
    }
}
