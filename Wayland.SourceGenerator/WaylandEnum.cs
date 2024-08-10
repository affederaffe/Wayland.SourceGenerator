using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class WaylandEnum
    {
        [DataMember(IsRequired = true)]
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; } = null!;

        [XmlAttribute(AttributeName = "since")]
        public int Since { get; set; }

        [XmlAttribute(AttributeName = "bitfield")]
        public bool IsBitfield { get; set; }

        [XmlElement(ElementName = "description")]
        public WaylandDescription? Description { get; set; }

        [DataMember(IsRequired = true)]
        [XmlElement(ElementName = "entry")]
        public WaylandEnumEntry[] Entries { get; set; } = null!;
    }
}
