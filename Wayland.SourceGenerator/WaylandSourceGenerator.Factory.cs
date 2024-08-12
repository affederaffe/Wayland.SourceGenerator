using System.Collections.Frozen;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static ClassDeclarationSyntax WithFactory(this ClassDeclarationSyntax cl, WaylandInterface wlInterface, WaylandProtocol wlProtocol, FrozenDictionary<string,WaylandProtocol> interfaceToProtocolDict)
        {
            TypeSyntax factoryInterfaceType = GenericName("IBindFactory")
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(
                            IdentifierName(cl.Identifier))));

            ClassDeclarationSyntax factoryClass = ClassDeclaration("ProxyFactory")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PrivateKeyword)))
                .WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(
                                factoryInterfaceType))))
                .AddMembers(
                    MethodDeclaration(
                            PointerType(
                                IdentifierName("WlInterface")),
                            "GetInterface")
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword)))
                        .WithBody(
                            Block(
                                ReturnStatement(
                                    GetWlInterfaceAddressFor(wlInterface.Name, wlProtocol, interfaceToProtocolDict)))),
                    MethodDeclaration(
                            IdentifierName(cl.Identifier),
                            "Create")
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword)))
                        .AddParameterListParameters(
                            Parameter(
                                    Identifier("handle"))
                                .WithType(
                                    IdentifierName("IntPtr")),
                            Parameter(
                                    Identifier("version"))
                                .WithType(
                                    PredefinedType(
                                        Token(
                                            SyntaxKind.IntKeyword))))
                        .WithBody(
                            Block(
                                ReturnStatement(
                                    ObjectCreationExpression(
                                            IdentifierName(cl.Identifier))
                                        .AddArgumentListArguments(
                                            Argument(
                                                IdentifierName("handle")),
                                            Argument(
                                                IdentifierName("version")))))));

            PropertyDeclarationSyntax factoryProperty = PropertyDeclaration(factoryInterfaceType, "BindFactory")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.InternalKeyword),
                        Token(SyntaxKind.StaticKeyword)))
                .WithAccessorList(
                    AccessorList(
                        SingletonList(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    Token(SyntaxKind.SemicolonToken)))))
                .WithInitializer(
                    EqualsValueClause(
                        InvocationExpression(
                            ObjectCreationExpression(
                                IdentifierName("ProxyFactory")))))
                .WithSemicolonToken(
                    Token(
                        SyntaxKind.SemicolonToken));

            return cl.AddMembers(factoryClass, factoryProperty);
        }
    }
}
