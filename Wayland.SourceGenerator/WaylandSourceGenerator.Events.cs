using System;
using System.Collections.Frozen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static ClassDeclarationSyntax WithEvents(this ClassDeclarationSyntax cl, WaylandInterface wlInterface, WaylandProtocol wlProtocol, FrozenDictionary<string, WaylandProtocol> interfaceToProtocolDict)
        {
            if (wlInterface.Events is null || wlInterface.Events.Length == 0)
            {
                return cl.AddMembers(
                    MakeDispatchEventMethod()
                        .WithBody(
                            Block()));
            }

            InterfaceDeclarationSyntax eventInterface = InterfaceDeclaration("IEvents")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.InternalKeyword)));

            SwitchStatementSyntax switchStatement= SwitchStatement(
                IdentifierName("opcode"));

            for (int eventIndex = 0; eventIndex < wlInterface.Events.Length; eventIndex++)
            {
                WaylandEvent wlEvent = wlInterface.Events[eventIndex];

                if (wlEvent.Arguments is null)
                    continue;

                SeparatedSyntaxList<ArgumentSyntax> arguments = [];
                SeparatedSyntaxList<ParameterSyntax> parameters = [];

                for (int argIndex = 0; argIndex < wlEvent.Arguments!.Length; argIndex++)
                {
                    WaylandArgument wlArgument = wlEvent.Arguments[argIndex];
                    ExpressionSyntax argumentAt = ElementAccessExpression(
                        IdentifierName("arguments"),
                        BracketedArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    MakeLiteralExpression(argIndex)))));

                    TypeSyntax? parameterType;
                    ExpressionSyntax? argument;

                    switch (wlArgument.Type)
                    {
                        case WaylandArgumentTypes.Int32:
                        case WaylandArgumentTypes.Uint32:
                        case WaylandArgumentTypes.FileDescriptor:
                        case WaylandArgumentTypes.Fixed:
                            parameterType = wlArgument.Type switch
                            {
                                WaylandArgumentTypes.Int32 or WaylandArgumentTypes.FileDescriptor => PredefinedType(Token(SyntaxKind.IntKeyword)),
                                WaylandArgumentTypes.Uint32 => PredefinedType(Token(SyntaxKind.UIntKeyword)),
                                WaylandArgumentTypes.Fixed => IdentifierName("WlFixed"),
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            string fieldName = wlArgument.Type switch
                            {
                                WaylandArgumentTypes.Int32 or WaylandArgumentTypes.FileDescriptor => "Int32",
                                WaylandArgumentTypes.Uint32 => "UInt32",
                                WaylandArgumentTypes.Fixed => "WlFixed",
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            argument = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentAt, IdentifierName(fieldName));

                            if (wlArgument.Enum is not null)
                            {
                                parameterType = GetQualifiedEnumType(wlArgument.Enum, wlProtocol, interfaceToProtocolDict);
                                argument = CastExpression(parameterType, argument);
                            }

                            break;
                        case WaylandArgumentTypes.String:
                            parameterType = PredefinedType(Token(SyntaxKind.StringKeyword));
                            if (wlArgument.AllowNull)
                                parameterType = NullableType(parameterType);
                            argument = InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Marshal"),
                                        IdentifierName("PtrToStringAnsi")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentAt, IdentifierName("IntPtr"))))));
                            break;
                        case WaylandArgumentTypes.Array:
                            TypeSyntax arrayType = GetUnknownArrayElementType(wlProtocol, wlInterface, wlEvent.Name, wlArgument.Name);
                            parameterType = GenericName("ReadOnlySpan")
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList(arrayType)));
                            argument = InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("WlArray"),
                                    GenericName("SpanFromWlArrayPtr")
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList(arrayType)))))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentAt, IdentifierName("IntPtr"))))));
                            break;
                        case WaylandArgumentTypes.Object:
                            parameterType = wlArgument.Interface is null
                                ? IdentifierName("WlProxy")
                                : GetQualifiedClassType(wlArgument.Interface, wlProtocol, interfaceToProtocolDict);

                            if (wlArgument.AllowNull)
                                parameterType = NullableType(parameterType);

                            argument = InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("WlProxy"),
                                        GenericName("FromNative")
                                            .AddTypeArgumentListArguments(parameterType)))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentAt, IdentifierName("IntPtr"))))));
                            break;
                        case WaylandArgumentTypes.NewId:
                            parameterType = GetQualifiedClassType(wlArgument.Interface!, wlProtocol, interfaceToProtocolDict);
                            argument = ObjectCreationExpression(parameterType)
                                .AddArgumentListArguments(
                                    Argument(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argumentAt, IdentifierName("IntPtr"))),
                                    Argument(
                                        IdentifierName("Version")));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    parameters = parameters.Add(
                        Parameter(
                                Identifier(
                                    SanitizeIdentifier(wlArgument.Name)))
                            .WithType(parameterType));

                    arguments = arguments.Add(
                        Argument(argument));
                }

                string eventName = Pascalize(wlEvent.Name.AsSpan());

                MethodDeclarationSyntax method = MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        eventName)
                    .WithParameterList(
                        ParameterList(parameters));

                if (wlEvent.DeprecatedSince > 0)
                    method = method.WithObsoleteAttribute();

                method = method.WithSummary(wlEvent.Description)
                    .WithSemicolonToken(
                        Token(SyntaxKind.SemicolonToken));

                eventInterface = eventInterface.AddMembers(method);

                switchStatement = switchStatement.AddSections(
                    SwitchSection()
                        .AddLabels(
                            CaseSwitchLabel(
                                MakeLiteralExpression(eventIndex)))
                        .AddStatements(
                            ExpressionStatement(
                                ConditionalAccessExpression(
                                    IdentifierName("Events"),
                                    InvocationExpression(
                                            MemberBindingExpression(
                                                IdentifierName(eventName)))
                                        .WithArgumentList(
                                            ArgumentList(arguments)))),
                            BreakStatement()));
            }

            PropertyDeclarationSyntax eventProperty = MakeGetSetProperty(
                NullableType(
                    IdentifierName("IEvents")),
                "Events",
                Token(SyntaxKind.InternalKeyword));

            MethodDeclarationSyntax dispatchEventMethod = MakeDispatchEventMethod()
                .WithBody(Block(switchStatement));

            return cl.AddMembers(
                eventInterface,
                eventProperty,
                dispatchEventMethod);

            static MethodDeclarationSyntax MakeDispatchEventMethod() =>
                MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        "DispatchEvent")
                    .AddModifiers(
                        Token(SyntaxKind.ProtectedKeyword),
                        Token(SyntaxKind.OverrideKeyword))
                    .AddParameterListParameters(
                        Parameter(
                                Identifier("opcode"))
                            .WithType(
                                PredefinedType(
                                    Token(SyntaxKind.UIntKeyword))),
                        Parameter(
                                Identifier("arguments"))
                            .WithType(
                                PointerType(
                                    IdentifierName("WlArgument"))));
        }
    }
}   
