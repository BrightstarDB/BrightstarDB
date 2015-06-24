namespace BrightstarDB.CodeGeneration
{
    using System;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using VB = Microsoft.CodeAnalysis.VisualBasic;
    using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

    // stuff that isn't on SyntaxGenerator that probably should be
    // might PR some of this stuff back to Roslyn at some point
    internal static class SyntaxGeneratorExtensions
    {
        public static SyntaxNode TypeOf(this SyntaxGenerator @this, SyntaxNode type, Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    return SyntaxFactory.TypeOfExpression((TypeSyntax)type);
                case Language.VisualBasic:
                    return VB.SyntaxFactory.GetTypeExpression((VBSyntax.TypeSyntax)type);
                default:
                    throw new NotSupportedException();
            }
        }

        public static SyntaxNode WithLeadingComments(this SyntaxGenerator @this, SyntaxNode node, string comments, Language language)
        {
            comments = comments
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(x => language == Language.CSharp ? "// " + x : "' " + x)
                .Aggregate(new StringBuilder(), (sb, next) => sb.AppendLine(next), x => x.ToString());

            node = node.WithLeadingTrivia(
                language == Language.CSharp
                    ? SyntaxFactory.ParseLeadingTrivia(comments)
                    : VB.SyntaxFactory.ParseLeadingTrivia(comments));

            return node;
        }
    }
}