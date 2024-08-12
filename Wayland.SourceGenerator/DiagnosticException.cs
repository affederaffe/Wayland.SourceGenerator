using System;

using Microsoft.CodeAnalysis;


namespace Wayland.SourceGenerator
{
    public sealed class DiagnosticException(Diagnostic diagnostic) : Exception
    {
        public Diagnostic Diagnostic { get; } = diagnostic;
    }
}
