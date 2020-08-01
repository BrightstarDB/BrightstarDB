using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace BrightstarDB.CodeGen.Generators
{

    public class EntityClassGenerator : ICodeGenerator
    {
        private readonly string identifier;

        public EntityClassGenerator(AttributeData attributeData)
        {
            attributeData.IfRequestedLaunchDebugger();
            identifier = (string)attributeData.ConstructorArguments[0].Value;
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            // Our generator is applied to any class that our attribute is applied to.
            var entityInterface = (InterfaceDeclarationSyntax) context.ProcessingNode;
            var entityName = entityInterface.Identifier.ValueText;
            var className = entityName.StartsWith("I") ? entityName.Substring(1) : entityName + "Impl";
            var results = SingletonList<MemberDeclarationSyntax>(ClassDeclaration(className)
                .WithModifiers(
                    TokenList(
                        entityInterface.Modifiers.Add(Token(SyntaxKind.PartialKeyword))
                        ))
                .WithBaseList(
                    GenerateEntityBaseList(entityInterface))
                .WithMembers(
                    GenerateEntityClassMembersAsync(className, entityInterface))
            );
            return Task.FromResult(results);
        }

        private BaseListSyntax GenerateEntityBaseList(
            InterfaceDeclarationSyntax entityInterface)
        {
            var result = BaseList(
                SeparatedList<BaseTypeSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        SimpleBaseType(IdentifierName("BrightstarDB.EntityFramework.BrightstarEntityObject")),
                        Token(SyntaxKind.CommaToken),
                        SimpleBaseType(IdentifierName(entityInterface.Identifier))
                    }));
            return result;
        }

        private SyntaxList<MemberDeclarationSyntax> GenerateEntityClassMembersAsync(
            string className,
            InterfaceDeclarationSyntax entityInterface)
        {
            var results = new List<MemberDeclarationSyntax>();
            //foreach (var c in GenerateConstructors(entityName, entityInterface)) results.Add(c);
            results.AddRange(GenerateConstructors(className, entityInterface));
            return new SyntaxList<MemberDeclarationSyntax>(results);
        }

        private static readonly QualifiedNameSyntax EF = QualifiedName(IdentifierName("BrightstarDB"), IdentifierName("EntityFramework"));
        private static readonly QualifiedNameSyntax EntityContext = QualifiedName(EF, IdentifierName("BrightstarEntityContext"));
        private static string DataObject = "BrightstarDB.Client.IDataObject";
        private IEnumerable<MemberDeclarationSyntax> GenerateConstructors(string className, 
            InterfaceDeclarationSyntax entityInterface)
        {
            var result = new List<MemberDeclarationSyntax>();
            // Default no-args constructor
            result.Add(
                ConstructorDeclaration(Identifier(className))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithBody(Block()));
            result.Add(
                ConstructorDeclaration(Identifier(className))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(
                        ParameterList(
                            SingletonSeparatedList(Parameter(Identifier("context")).WithType(EntityContext))))
                    .WithInitializer(
                        ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]
                                    {
                                        Argument(IdentifierName("context")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(TypeOfExpression(IdentifierName(className)))
                                    })
                            )))
                    .WithBody(Block()));
            return result;
        }
    }

    public static class GeneratorAttributeExtensions
    {
        internal static void IfRequestedLaunchDebugger(this AttributeData attributeData)
        {
            if (!attributeData.NamedArguments.Any(n => n.Key == "LaunchDebuggerDuringBuild" && n.Value.ToCSharpString() == "true")) return;
            Debugger.Launch();
            while (!Debugger.IsAttached) Thread.Sleep(500); // eww, eww, eww
        }
    }
}
