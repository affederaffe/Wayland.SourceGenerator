using System;
using System.Xml.Serialization;


namespace Wayland.SourceGenerator
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class WaylandEvent : WaylandProtocolMessage;
}
