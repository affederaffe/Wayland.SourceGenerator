using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static ClassDeclarationSyntax WithConstructor(this ClassDeclarationSyntax cl) =>
            cl.AddMembers(
                ConstructorDeclaration(cl.Identifier)
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.InternalKeyword)))
                    .AddParameterListParameters(
                        Parameter(
                                Identifier("handle"))
                            .WithType(
                                IdentifierName("IntPtr")),
                        Parameter(
                                Identifier("version"))
                            .WithType(
                                PredefinedType(
                                    Token(SyntaxKind.IntKeyword))))
                    .WithInitializer(
                        ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ArgumentList(
                            SeparatedList([
                                Argument(
                                    IdentifierName("handle")),
                                Argument(
                                    IdentifierName("version"))]))))
                    .WithBody(
                        Block()));
    }
}
