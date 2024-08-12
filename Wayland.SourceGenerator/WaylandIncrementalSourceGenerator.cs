using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    [Generator]
    public class WaylandIncrementalSourceGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            XmlSerializer xmlSerializer = new(typeof(WaylandProtocol));

            IncrementalValuesProvider<WaylandProtocol> generatorProvider = context.AdditionalTextsProvider
                .Where(static x => x.Path.EndsWith(".xml", StringComparison.Ordinal))
                .Select((additionalText, _) =>
                {
                    if (additionalText.GetText() is not { } text)
                        return null;
                    using StringReader reader = new(text.ToString());
                    using XmlReader xmlReader = XmlReader.Create(reader);
                    return xmlSerializer.Deserialize(xmlReader) as WaylandProtocol;
                })
                .Where(static x => x is not null)!;

            context.RegisterSourceOutput(generatorProvider.Collect(), static (productionContext, protocols) =>
            {
                try
                {
                    int allInterfacesLength = GetInterfacesCount(protocols);
                    FrozenDictionary<string, WaylandProtocol> interfaceToProtocolDict = CreateInterfaceToProtocolDict(protocols, allInterfacesLength);
                    (string, CompilationUnitSyntax)[] sources = new (string, CompilationUnitSyntax)[allInterfacesLength];

                    int j = 0;
                    foreach (WaylandProtocol wlProtocol in protocols)
                    {
                        SyntaxToken nsName = WaylandSourceGenerator.GetNamespaceName(wlProtocol);
                        foreach (WaylandInterface wlInterface in wlProtocol.Interfaces)
                        {
                            SyntaxToken typeName = WaylandSourceGenerator.GetClassTypeName(wlInterface.Name);
                            ClassDeclarationSyntax cl = ClassDeclaration(typeName)
                                .AddModifiers(
                                    Token(SyntaxKind.InternalKeyword),
                                    Token(SyntaxKind.SealedKeyword),
                                    Token(SyntaxKind.UnsafeKeyword),
                                    Token(SyntaxKind.PartialKeyword))
                                .WithBaseList(
                                    BaseList(
                                        SingletonSeparatedList<BaseTypeSyntax>(
                                            SimpleBaseType(
                                                IdentifierName("WlProxy")))))
                                .WithSummary(wlInterface.Description)
                                .WithSignature(wlInterface, wlProtocol, interfaceToProtocolDict)
                                .WithConstructor()
                                .WithEnums(wlInterface)
                                .WithEvents(wlInterface, wlProtocol, interfaceToProtocolDict)
                                .WithRequests(wlInterface, wlProtocol, interfaceToProtocolDict)
                                .WithFactory(wlInterface, wlProtocol, interfaceToProtocolDict);
                            NamespaceDeclarationSyntax ns = NamespaceDeclaration(
                                    IdentifierName(nsName))
                                .WithMembers(
                                    SingletonList<MemberDeclarationSyntax>(cl));
                            sources[j++] = ($"{nsName.Text}.{typeName.Text}.g.cs", WaylandSourceGenerator.MakeCompilationUnit(ns));
                        }
                    }

                    productionContext.AddSource("Wayland.SourceGenerator.WlDisplay.g.cs", WaylandSourceGenerator.WlDisplayClass);
                    productionContext.AddSource("Wayland.SourceGenerator.WlRegistry.g.cs", WaylandSourceGenerator.WlRegistryClass);

                    foreach ((string HintName, CompilationUnitSyntax CompilationUnit) source in sources)
                        productionContext.AddSource(source.HintName, source.CompilationUnit.GetText(Encoding.UTF8));
                }
                catch (DiagnosticException e)
                {
                    productionContext.ReportDiagnostic(e.Diagnostic);
                }
            });

            context.RegisterPostInitializationOutput(static initializationContext =>
            {
                initializationContext.AddSource("Wayland.SourceGenerator.LibWayland.g.cs", WaylandSourceGenerator.LibWaylandClass);
                initializationContext.AddSource("Wayland.SourceGenerator.WaylandException.g.cs", WaylandSourceGenerator.WaylandExceptionClass);
                initializationContext.AddSource("Wayland.SourceGenerator.IBindFactory.g.cs", WaylandSourceGenerator.BindFactoryInterface);
                initializationContext.AddSource("Wayland.SourceGenerator.WlProxy.g.cs", WaylandSourceGenerator.WlProxyClass);
                initializationContext.AddSource("Wayland.SourceGenerator.WlProxyWrapper.g.cs", WaylandSourceGenerator.WlProxyWrapperClass);
                initializationContext.AddSource("Wayland.SourceGenerator.WlEventQueue.g.cs", WaylandSourceGenerator.WlEventQueueClass);
                initializationContext.AddSource("Wayland.SourceGenerator.Utf8Buffer.g.cs", WaylandSourceGenerator.Utf8BufferClass);
                initializationContext.AddSource("Wayland.SourceGenerator.WlInterface.g.cs", WaylandSourceGenerator.WlInterfaceStruct);
                initializationContext.AddSource("Wayland.SourceGenerator.WlMessage.g.cs", WaylandSourceGenerator.WlMessageStruct);
                initializationContext.AddSource("Wayland.SourceGenerator.WlArgument.g.cs", WaylandSourceGenerator.WlArgumentStruct);
                initializationContext.AddSource("Wayland.SourceGenerator.WlArray.g.cs", WaylandSourceGenerator.WlArrayStruct);
                initializationContext.AddSource("Wayland.SourceGenerator.WlFixed.g.cs", WaylandSourceGenerator.WlFixedStruct);
            });
        }

        private static FrozenDictionary<string, WaylandProtocol> CreateInterfaceToProtocolDict(ImmutableArray<WaylandProtocol> wlProtocols, int allInterfacesLength)
        {
            Dictionary<string, WaylandProtocol> mutableInterfaceToProtocolDict = new(allInterfacesLength);
            foreach (WaylandProtocol wlProtocol in wlProtocols)
            {
                foreach (WaylandInterface wlInterface in wlProtocol.Interfaces)
                {
                    if (mutableInterfaceToProtocolDict.ContainsKey(wlInterface.Name))
                        throw new DiagnosticException(WaylandSourceGenerator.MakeDuplicateProtocolDefinitionDiagnostic(wlInterface.Name));
                    mutableInterfaceToProtocolDict.Add(wlInterface.Name, wlProtocol);
                }
            }

            return mutableInterfaceToProtocolDict.ToFrozenDictionary();
        }

        private static int GetInterfacesCount(ImmutableArray<WaylandProtocol> protocols)
        {
            int sum = 0;
            foreach (WaylandProtocol protocol in protocols)
                sum += protocol.Interfaces.Length;
            return sum;
        }
    }
}
