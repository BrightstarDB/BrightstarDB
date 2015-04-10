namespace BrightstarDB.CodeGeneration.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;
    using NUnit.Framework;

    [TestFixture]
    public class GeneratorTests
    {
        [Test]
        [TestCase("EmptyEntity", Language.CSharp)]
        [TestCase("IdentifierPrecedence", Language.CSharp)]
        [TestCase("SupportedScalarPropertyTypes", Language.CSharp)]
        [TestCase("Relationships", Language.CSharp)]
        [TestCase("AttributePropagation", Language.CSharp)]
        public async Task TestCodeGeneration(string resourceBaseName, Language language)
        {
            var inputResourceName = "BrightstarDB.CodeGeneration.Tests." + resourceBaseName + "Input_" + language.ToString() + ".txt";
            var outputResourceName = "BrightstarDB.CodeGeneration.Tests." + resourceBaseName + "Output_" + language.ToString() + ".txt";

            using (var inputStream = this.GetType().Assembly.GetManifestResourceStream(inputResourceName))
            using (var outputStream = this.GetType().Assembly.GetManifestResourceStream(outputResourceName))
            using (var outputStreamReader = new StreamReader(outputStream))
            {
                var workspace = new AdhocWorkspace();
                var projectId = ProjectId.CreateNewId();
                var versionStamp = VersionStamp.Create();
                var projectInfo = ProjectInfo.Create(
                    projectId,
                    versionStamp,
                    "AdhocProject",
                    "AdhocProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[]
                    {
                        MetadataReference.CreateFromAssembly(typeof(object).Assembly),
                        MetadataReference.CreateFromAssembly(typeof(Uri).Assembly),
                        MetadataReference.CreateFromAssembly(typeof(BrightstarException).Assembly)
                    });
                var project = workspace.AddProject(projectInfo);
                workspace.AddDocument(projectId, "Source.cs", SourceText.From(inputStream));
                var solution = workspace.CurrentSolution;

                var results = await Generator
                    .GenerateAsync(
                        language,
                        solution,
                        "BrightstarDB.CodeGeneration.Tests",
                        interfacePredicate: x => true);
                var result = results
                    .Aggregate(
                        new StringBuilder(),
                        (current, next) => current.AppendLine(next.ToString()),
                        x => x.ToString());

                var expectedCode = outputStreamReader.ReadToEnd();

                // make sure version changes don't break the tests
                expectedCode = expectedCode.Replace("$VERSION$", typeof(BrightstarException).Assembly.GetName().Version.ToString());

                //// useful when converting generated code to something that can be pasted into an expectation file
                //var sanitisedResult = result.Replace("1.10.0.0", "$VERSION$");
                //System.Diagnostics.Debug.WriteLine(sanitisedResult);

                Assert.AreEqual(expectedCode, result);
            }
        }

        [Test]
        [TestCase("InvalidIdType", Language.CSharp, "The property 'BrightstarDB.CodeGeneration.Tests.IInvalidIdType.Id' must be of type string to be used as the identity property for an entity. If this property is intended to be the identity property for the entity please change its type to string. If it is not intended to be the identity property, either rename this property or create an identity property and decorate it with the [BrightstarDB.EntityFramework.IdentifierAttribute] attribute.")]
        [TestCase("IdWithSetter", Language.CSharp, "The property 'BrightstarDB.CodeGeneration.Tests.IIdWithSetter.Id' must not have a setter to be used as the identity property for an entity. If this property is intended to be the identity property for the entity please remove the setter. If it is not intended to be the identity property, either rename this property or create an identity propertyn and decorate it with the [BrightstarDB.EntityFramework.IdentifierAttribute] attribute.")]
        [TestCase("PropertyWithUnsupportedType", Language.CSharp, "Invalid property: BrightstarDB.CodeGeneration.Tests.IPropertyWithUnsupportedType.Property - the property type System.Action is not supported by Entity Framework.")]
        [TestCase("InvalidInversePropertyName", Language.CSharp, "Invalid BrightstarDB.EntityFramework.InversePropertyAttribute attribute on property BrightstarDB.CodeGeneration.Tests.IInvalidInversePropertyName_B.A. A property named 'B' cannot be found on the target interface type BrightstarDB.CodeGeneration.Tests.IInvalidInversePropertyName_A.")]
        public async Task TestErrorConditions(string resourceBaseName, Language language, string expectedErrorMessage)
        {
            var inputResourceName = "BrightstarDB.CodeGeneration.Tests." + resourceBaseName + "Input_" + language.ToString() + ".txt";

            using (var inputStream = this.GetType().Assembly.GetManifestResourceStream(inputResourceName))
            {
                var workspace = new AdhocWorkspace();
                var projectId = ProjectId.CreateNewId();
                var versionStamp = VersionStamp.Create();
                var projectInfo = ProjectInfo.Create(
                    projectId,
                    versionStamp,
                    "AdhocProject",
                    "AdhocProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[]
                    {
                        MetadataReference.CreateFromAssembly(typeof(object).Assembly),
                        MetadataReference.CreateFromAssembly(typeof(Uri).Assembly),
                        MetadataReference.CreateFromAssembly(typeof(BrightstarException).Assembly)
                    });
                var project = workspace.AddProject(projectInfo);
                workspace.AddDocument(projectId, "Source.cs", SourceText.From(inputStream));
                var solution = workspace.CurrentSolution;

                try
                {
                    var results = await Generator
                        .GenerateAsync(
                            language,
                            solution,
                            "BrightstarDB.CodeGeneration.Tests",
                            interfacePredicate: x => true);

                    Assert.Fail("No exception was thrown during code generation.");
                }
                catch (Exception ex)
                {
                    Assert.AreEqual(expectedErrorMessage, ex.Message);
                }
            }
        }
    }
}