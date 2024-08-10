using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Wayland.SourceGenerator
{
    public static partial class WaylandSourceGenerator
    {
        internal static T WithSummary<T>(this T member, WaylandDescription? description) where T : MemberDeclarationSyntax =>
            member.WithSummary(description?.Text);

        private static T WithSummary<T>(this T member, string? description) where T : MemberDeclarationSyntax =>
            description is null || description.Length == 0
                ? member
                : member.WithLeadingTrivia(
                    Trivia(
                        DocumentationComment(
                                MakeSummary(description),
                            XmlText(
                                XmlTextNewLine("\n", false)))));

        private static XmlElementSyntax MakeSummary(string summary)
        {
            ReadOnlySpan<string> lines = summary.Split('\n').AsSpan();
            if (lines.Length == 0)
                return XmlSummaryElement();

            lines = TrimLines(lines);
            XmlNodeSyntax[] nodes = new XmlNodeSyntax[lines.Length * 2 + 1];
            nodes[0] = XmlNewLine("\n");
            nodes[nodes.Length - 1] = XmlNewLine("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                nodes[i * 2 + 1] = XmlText($" {lines[i].Trim()}");
                if (i * 2 + 2 < nodes.Length)
                    nodes[i * 2 + 2] = i != 0 && i != lines.Length - 1 ? XmlNewLine("<br/>\n") : XmlNewLine("\n");
            }

            return XmlSummaryElement(nodes);
        }

        private static ReadOnlySpan<string> TrimLines(ReadOnlySpan<string> lines)
        {
            int i = 0;
            while (lines.Length > i && string.IsNullOrWhiteSpace(lines[i]))
                i++;
            int j = lines.Length - 1;
            while (j >= i && string.IsNullOrWhiteSpace(lines[j]))
                j--;
            return lines.Slice(i, j - i + 1);
        }
    }
}
