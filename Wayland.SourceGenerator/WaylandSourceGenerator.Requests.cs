using System;
using System.Collections.Frozen;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static ClassDeclarationSyntax WithRequests(this ClassDeclarationSyntax cl, WaylandInterface wlInterface, WaylandProtocol wlProtocol, FrozenDictionary<string, WaylandProtocol> interfaceToProtocolDict)
        {
            if (wlInterface.Requests is null)
                return cl;

            for (int requestIndex = 0; requestIndex < wlInterface.Requests.Length; requestIndex++)
            {
                WaylandRequest wlRequest = wlInterface.Requests[requestIndex];

                WaylandArgument? newIdArgument = wlRequest.Arguments?.FirstOrDefault(static arg => arg.Type == WaylandArgumentTypes.NewId);
                if (newIdArgument is not null && newIdArgument.Interface is null)
                    continue;

                bool isCtor = newIdArgument is not null;
                TypeSyntax ctorType = newIdArgument is not null
                    ? IdentifierName(
                        GetClassTypeName(newIdArgument.Interface!))
                    : PredefinedType(
                        Token(SyntaxKind.VoidKeyword));

                SeparatedSyntaxList<ParameterSyntax> parameters = [];
                SeparatedSyntaxList<ExpressionSyntax> arguments = [];
                SeparatedSyntaxList<StatementSyntax> statements = [];
                SeparatedSyntaxList<IfStatementSyntax> nullChecks = [];
                SeparatedSyntaxList<StatementSyntax> callStatements = [];
                SeparatedSyntaxList<VariableDeclarationSyntax> fixedStatements = [];

                if (wlRequest.Since > 0)
                {
                    statements = statements.Add(
                        IfStatement(
                            BinaryExpression(SyntaxKind.LessThanExpression,
                                IdentifierName("Version"), MakeLiteralExpression(wlRequest.Since)),
                            ThrowStatement(
                                ObjectCreationExpression(
                                        ParseTypeName("InvalidOperationException"))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    MakeLiteralExpression(
                                                        $"Request {wlRequest.Name} is only supported since version {wlRequest.Since}"))))))));
                }

                if (wlRequest.Arguments is not null)
                {
                    foreach (WaylandArgument wlArgument in wlRequest.Arguments)
                    {
                        TypeSyntax? parameterType = null;
                        string argumentName = Pascalize(
                            SanitizeIdentifier(wlArgument.Name).AsSpan(), true);

                        switch (wlArgument.Type)
                        {
                            case WaylandArgumentTypes.Int32:
                            case WaylandArgumentTypes.Fixed:
                            case WaylandArgumentTypes.FileDescriptor:
                            case WaylandArgumentTypes.Uint32:
                                parameterType = wlArgument.Type switch
                                {
                                    WaylandArgumentTypes.Int32 or WaylandArgumentTypes.FileDescriptor => PredefinedType(Token(SyntaxKind.IntKeyword)),
                                    WaylandArgumentTypes.Uint32 => PredefinedType(Token(SyntaxKind.UIntKeyword)),
                                    WaylandArgumentTypes.Fixed => IdentifierName("WlFixed"),
                                    _ => throw new ArgumentOutOfRangeException()
                                };

                                if (wlArgument.Enum is not null)
                                {
                                    arguments = arguments.Add(
                                        CastExpression(
                                            parameterType, IdentifierName(argumentName)));
                                    parameterType = GetQualifiedEnumType(wlArgument.Enum, wlProtocol, interfaceToProtocolDict);
                                }
                                else
                                {
                                    arguments = arguments.Add(
                                        IdentifierName(argumentName));
                                }

                                break;
                            case WaylandArgumentTypes.String:
                                parameterType = PredefinedType(Token(SyntaxKind.StringKeyword));
                                if (wlArgument.AllowNull)
                                {
                                    parameterType = NullableType(parameterType);
                                    nullChecks = nullChecks.Add(
                                        MakeNullCheck(argumentName));
                                }

                                string utf8BufferVarName = $"{argumentName}Utf8Buffer";
                                arguments = arguments.Add(
                                    IdentifierName(utf8BufferVarName));
                                TypeSyntax bufferType = IdentifierName("Utf8Buffer");
                                statements = statements.Add(
                                    LocalDeclarationStatement(
                                            VariableDeclaration(bufferType)
                                                .WithVariables(
                                                    SingletonSeparatedList(
                                                        VariableDeclarator(utf8BufferVarName)
                                                            .WithInitializer(
                                                                EqualsValueClause(
                                                                    ObjectCreationExpression(bufferType)
                                                                        .WithArgumentList(
                                                                            ArgumentList(
                                                                                SingletonSeparatedList(
                                                                                    Argument(
                                                                                        IdentifierName(argumentName))))))))))
                                        .WithModifiers(
                                            SyntaxTokenList.Create(
                                                Token(SyntaxKind.UsingKeyword))));
                                break;
                            case WaylandArgumentTypes.Array:
                                TypeSyntax arrayElementType = GetUnknownArrayElementType(wlProtocol, wlInterface, wlRequest.Name, wlArgument.Name);
                                parameterType = GenericName("ReadOnlySpan")
                                    .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SingletonSeparatedList(
                                                arrayElementType)));
                                string pointerName = $"{argumentName}Pointer";
                                string wlArrayName = $"{argumentName}WlArray";
                                fixedStatements = fixedStatements.Add(
                                    VariableDeclaration(
                                        PointerType(arrayElementType),
                                        SingletonSeparatedList(
                                            VariableDeclarator(pointerName)
                                                .WithInitializer(
                                                    EqualsValueClause(
                                                        IdentifierName(argumentName))))));
                                callStatements = callStatements.Add(
                                    LocalDeclarationStatement(
                                        VariableDeclaration(
                                                IdentifierName("WlArray"))
                                            .WithVariables(
                                                SingletonSeparatedList(
                                                    VariableDeclarator(wlArrayName)
                                                        .WithInitializer(
                                                            EqualsValueClause(
                                                                InvocationExpression(
                                                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName("WlArray"), IdentifierName("FromPointer")))
                                                                    .AddArgumentListArguments(
                                                                        Argument(
                                                                            IdentifierName(pointerName)),
                                                                        Argument(
                                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                                IdentifierName(argumentName), IdentifierName("Length"))))))))));
                                arguments = arguments.Add(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(wlArrayName)));
                                break;
                            case WaylandArgumentTypes.Object:
                                parameterType = GetQualifiedClassType(wlArgument.Interface!, wlProtocol, interfaceToProtocolDict);

                                if (wlArgument.AllowNull)
                                {
                                    parameterType = NullableType(parameterType);
                                    nullChecks = nullChecks.Add(
                                        MakeNullCheck(argumentName));
                                }

                                arguments = arguments.Add(
                                    IdentifierName(argumentName));
                                break;
                            case WaylandArgumentTypes.NewId:
                                arguments = arguments.Add(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("WlArgument"), IdentifierName("NewId")));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (parameterType is not null)
                        {
                            parameters = parameters.Add(
                                Parameter(
                                        Identifier(argumentName))
                                    .WithType(parameterType));
                        }
                    }
                }

                callStatements = callStatements.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                                PointerType(
                                    IdentifierName("WlArgument")))
                            .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator("__args")
                                        .WithInitializer(
                                            EqualsValueClause(
                                                StackAllocArrayCreationExpression(
                                                    ArrayType(
                                                            IdentifierName("WlArgument"))
                                                        .WithRankSpecifiers(
                                                            SingletonList(
                                                                ArrayRankSpecifier())),
                                                    InitializerExpression(SyntaxKind.ArrayInitializerExpression, arguments))))))));

                ArgumentListSyntax args = ArgumentList()
                    .AddArguments(
                        Argument(
                            IdentifierName("Handle")),
                        Argument(
                            MakeLiteralExpression(requestIndex)),
                        Argument(
                            IdentifierName("__args")));

                if (isCtor)
                {
                    args = args.AddArguments(
                        Argument(
                            GetWlInterfaceRefFor(newIdArgument!.Interface!, wlProtocol, interfaceToProtocolDict)),
                        Argument(
                            CastExpression(
                                PredefinedType(
                                    Token(SyntaxKind.UIntKeyword)),
                                GetWlInterfaceVersionFor(newIdArgument.Interface!, wlProtocol, interfaceToProtocolDict))));
                }

                InvocationExpressionSyntax callExpr = InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("LibWayland"), IdentifierName(isCtor ? "wl_proxy_marshal_array_constructor_versioned" : "wl_proxy_marshal_array")))
                    .WithArgumentList(args);

                if (isCtor)
                {
                    callStatements = callStatements.Add(
                        LocalDeclarationStatement(
                            VariableDeclaration(
                                    IdentifierName("IntPtr"))
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator("__ret")
                                            .WithInitializer(
                                                EqualsValueClause(callExpr))))));
                    callStatements = callStatements.Add(
                        ReturnStatement(
                            ConditionalExpression(
                                BinaryExpression(SyntaxKind.EqualsExpression,
                                    IdentifierName("__ret"), MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("IntPtr"), IdentifierName("Zero"))),
                                MakeNullLiteralExpression(),
                                ObjectCreationExpression(ctorType)
                                    .AddArgumentListArguments(
                                        Argument(
                                            IdentifierName("__ret")),
                                        Argument(
                                            GetWlInterfaceVersionFor(newIdArgument!.Interface!, wlProtocol, interfaceToProtocolDict))))));
                }
                else
                {
                    callStatements = callStatements.Add(
                        ExpressionStatement(callExpr));

                    if (wlRequest.Type == "destructor")
                    {
                        callStatements = callStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName("DestroyProxy"))));
                    }
                }

                if (fixedStatements.Count == 0)
                {
                    statements = statements.AddRange(callStatements);
                }
                else
                {
                    StatementSyntax callBlock = Block(callStatements);
                    callBlock = fixedStatements.Reverse().Aggregate(callBlock, static (current, fixedDeclaration) => FixedStatement(fixedDeclaration, current));
                    statements = statements.Add(callBlock);
                }

                MethodDeclarationSyntax method = MethodDeclaration(ctorType, Pascalize(wlRequest.Name.AsSpan()))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.InternalKeyword)))
                    .WithSummary(wlRequest.Description)
                    .WithParameterList(
                        ParameterList(parameters))
                    .WithBody(
                        Block(statements));

                if (wlRequest.DeprecatedSince > 0)
                    method = method.WithObsoleteAttribute();

                cl = cl.AddMembers(method);
            }

            return cl;
        }
    }
}
