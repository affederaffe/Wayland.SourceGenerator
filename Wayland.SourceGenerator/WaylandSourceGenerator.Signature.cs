using System;
using System.Collections.Frozen;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        public static ClassDeclarationSyntax WithSignature(this ClassDeclarationSyntax cl, WaylandInterface wlInterface, FrozenDictionary<string, WaylandProtocol> interfaceToProtocolDict)
        {
            AttributeListSyntax attributeList = AttributeList(
                SingletonSeparatedList(
                    Attribute(
                        IdentifierName("FixedAddressValueType"))));

            FieldDeclarationSyntax signatureField = FieldDeclaration(
                    VariableDeclaration(
                            IdentifierName("WlInterface"))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator("WlInterface"))))
                .WithAttributeLists(
                    SingletonList(attributeList))
                .AddModifiers(
                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.StaticKeyword));

            ArgumentListSyntax args = ArgumentList(
                SeparatedList([
                        Argument(
                        MakeLiteralExpression(wlInterface.Name)),
                    Argument(
                        MakeLiteralExpression(wlInterface.Version)),
                    GenerateWlMessageList(wlInterface.Requests, interfaceToProtocolDict),
                    GenerateWlMessageList(wlInterface.Events, interfaceToProtocolDict)]));

            ConstructorDeclarationSyntax staticCtor = ConstructorDeclaration(cl.Identifier)
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.StaticKeyword)))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(cl.Identifier),
                                        IdentifierName("WlInterface")),
                                    ObjectCreationExpression(
                                            IdentifierName("WlInterface"))
                                        .WithArgumentList(args))))));

            MethodDeclarationSyntax method = MethodDeclaration(
                PointerType(
                    IdentifierName("WlInterface")),
                "GetWlInterface")
                .AddModifiers(
                    Token(SyntaxKind.ProtectedKeyword),
                    Token(SyntaxKind.OverrideKeyword))
                .WithBody(
                    Block(
                        SingletonList<StatementSyntax>(
                            ReturnStatement(
                                GetWlInterfaceAddressFor(wlInterface.Name, interfaceToProtocolDict)))));

            cl = cl.AddMembers(signatureField, staticCtor, method);

            return cl;
        }

        private static ArgumentSyntax GenerateWlMessageList(ReadOnlySpan<WaylandProtocolMessage> messages, FrozenDictionary<string, WaylandProtocol> interfaceToProtocolDict)
        {
            SeparatedSyntaxList<ExpressionSyntax> elements = [];

            foreach (WaylandProtocolMessage msg in messages)
                elements = elements.Add(GenerateWlMessage(msg, interfaceToProtocolDict));

            return Argument(
                ArrayCreationExpression(
                    ArrayType(
                        IdentifierName("WlMessage"))
                        .WithRankSpecifiers(
                            SingletonList(
                                ArrayRankSpecifier())),
                    InitializerExpression(SyntaxKind.ArrayInitializerExpression, elements)));
        }

        private static ObjectCreationExpressionSyntax GenerateWlMessage(WaylandProtocolMessage wlMessage, FrozenDictionary<string, WaylandProtocol> interfaceToProtocolDict)
        {
            StringBuilder signature = new();

            if (wlMessage.Since != 0)
                signature.Append(wlMessage.Since);

            SeparatedSyntaxList<ExpressionSyntax> interfaceList = [];

            if (wlMessage.Arguments is not null)
            {
                foreach (WaylandArgument arg in wlMessage.Arguments)
                {
                    if (arg.AllowNull)
                        signature.Append('?');

                    if (arg is { Type: WaylandArgumentTypes.NewId, Interface: null })
                    {
                        signature.Append("su");
                        interfaceList = interfaceList.AddRange([
                            MakeNullLiteralExpression(),
                            MakeNullLiteralExpression()]);
                    }

                    signature.Append(WaylandArgumentTypes.NamesToCodes[arg.Type]);

                    interfaceList = interfaceList.Add(
                        arg.Interface is not null
                            ? GetWlInterfaceAddressFor(arg.Interface, interfaceToProtocolDict)
                            : MakeNullLiteralExpression());
                }
            }

            ArgumentListSyntax argList = ArgumentList(
                SeparatedList([
                    Argument(
                        MakeLiteralExpression(wlMessage.Name)),
                    Argument(
                        MakeLiteralExpression(signature.ToString())),
                    Argument(
                        ArrayCreationExpression(
                                ArrayType(
                                        PointerType(
                                            IdentifierName("WlInterface")))
                                    .WithRankSpecifiers(
                                        SingletonList(
                                            ArrayRankSpecifier())))
                            .WithInitializer(
                                InitializerExpression(SyntaxKind.ArrayInitializerExpression, interfaceList)))]));

            return ObjectCreationExpression(
                    IdentifierName("WlMessage"), argList, null)
                .WithLeadingTrivia(CarriageReturn);
        }
    }
}
