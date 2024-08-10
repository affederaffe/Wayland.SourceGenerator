using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Wayland.SourceGenerator
{
    public partial class WaylandSourceGenerator
    {
        private static readonly List<TypeNameHint> _arrayTypeHints = [
            new TypeNameHint("wayland", "wl_keyboard", "enter", "keys", PredefinedType(Token(SyntaxKind.IntKeyword))),
            new TypeNameHint("xdg_shell", "xdg_toplevel", "configure", "states", IdentifierName("XdgShell.XdgToplevel.StateEnum")),
            new TypeNameHint("xdg_shell", "xdg_toplevel", "wm_capabilities", "capabilities", IdentifierName("XdgShell.XdgToplevel.WmCapabilitiesEnum"))
        ];

        private static TypeSyntax GetUnknownArrayElementType(WaylandProtocol wlProtocol, WaylandInterface wlInterface, string messageName, string argumentName) =>
            _arrayTypeHints.FirstOrDefault(hint =>
                hint.Match(wlProtocol.Name, wlInterface.Name, messageName, argumentName))?.Type ??
            PredefinedType(
                Token(SyntaxKind.ByteKeyword));

        private sealed class TypeNameHint(string protocol, string @interface, string message, string argument, TypeSyntax type)
        {
            private string Protocol { get; } = protocol;

            private string Interface { get; } = @interface;

            private string Message { get; } = message;

            private string Argument { get; } = argument;

            public TypeSyntax Type { get; } = type;

            public bool Match(string protocol, string @interface, string message, string argument)
                => Protocol == protocol
                   && Interface == @interface
                   && Message == message
                   && Argument == argument;
        }
    }
}
