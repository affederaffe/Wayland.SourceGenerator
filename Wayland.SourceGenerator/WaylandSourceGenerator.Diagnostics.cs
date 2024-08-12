using Microsoft.CodeAnalysis;


namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static Diagnostic MakeDuplicateProtocolDefinitionDiagnostic(string duplicateProtocolName) =>
            Diagnostic.Create(
                new DiagnosticDescriptor("WL0001",
                    "Duplicate protocol definition",
                    "Duplicate protocol definition found for protocol {0}",
                    string.Empty,
                    DiagnosticSeverity.Error,
                    true),
                null,
                duplicateProtocolName);

        private static Diagnostic MakeMissingProtocolDiagnostic(string protocolName, string missingDependencyName) =>
            Diagnostic.Create(
                new DiagnosticDescriptor("WL0002",
                    "Missing protocol dependency",
                    "Missing protocol dependency {0} for protocol {1}",
                    string.Empty,
                    DiagnosticSeverity.Error,
                    true),
                null,
                missingDependencyName, protocolName);
    }
}
