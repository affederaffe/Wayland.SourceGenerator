using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class WaylandDescription
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "summary")]
        public string Summary { get; set; } = null!;

        [DataMember(IsRequired = true)]
        [XmlText]
        public string Text { get; set; } = null!;
    }
}
