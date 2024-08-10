using System;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static ClassDeclarationSyntax WithEnums(this ClassDeclarationSyntax cl, WaylandInterface wlProtocol)
        {
            if (wlProtocol.Enums is null)
                return cl;

            foreach (WaylandEnum wlEnum in wlProtocol.Enums)
            {
                EnumDeclarationSyntax enumDeclaration = EnumDeclaration(
                   GetEnumTypeName(wlEnum.Name))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.InternalKeyword)))
                    .WithSummary(wlEnum.Description);

                if (wlEnum.IsBitfield)
                {
                    enumDeclaration = enumDeclaration.AddAttributeLists(
                        AttributeList(
                            SingletonSeparatedList(
                                Attribute(
                                    IdentifierName("Flags")))));
                }

                foreach (WaylandEnumEntry wlEnumEntry in wlEnum.Entries)
                {
                    string name = Pascalize(
                        SanitizeIdentifier(wlEnumEntry.Name)
                            .AsSpan());

                    EnumMemberDeclarationSyntax enumMember = EnumMemberDeclaration(name)
                        .WithEqualsValue(
                            EqualsValueClause(
                                ParseExpression(wlEnumEntry.Value)))
                        .WithSummary(wlEnumEntry.Summary);

                    if (wlEnumEntry.DeprecatedSince > 0)
                        enumMember = enumMember.WithObsoleteAttribute();

                    enumDeclaration = enumDeclaration.AddMembers(enumMember);
                }

                cl = cl.AddMembers(enumDeclaration);
            }

            return cl;
        }
    }
}
