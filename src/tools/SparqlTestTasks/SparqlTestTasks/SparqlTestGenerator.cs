using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using VDS.RDF.Parsing;
namespace SparqlTestTasks
{
    public class SparqlTestGenerator : Task
    {
        [Required]
        public ITaskItem[] ManifestFiles { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Required]
        public string Namespace { get; set; }

        public bool RenameTestFiles { get; set; }

        #region Overrides of Task

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            var outputDir = new DirectoryInfo(OutputDirectory);
            if (!outputDir.Exists) outputDir.Create();

            foreach (var manifestFile in ManifestFiles)
            {
                if (!File.Exists(manifestFile.ItemSpec))
                {
                    Log.LogError("The ManifestFile '{0}' could not be found", manifestFile.ItemSpec);
                    return false;
                }
                var testManifest = new TestManifest(manifestFile, outputDir, Namespace, RenameTestFiles);
                if (testManifest.IsUpToDate())
                {
                    Log.LogMessage(MessageImportance.Normal,
                                   "The target file {0} is up to date with respect to the manifest file {1}.",
                                   testManifest.OutputFilePath, testManifest.ManifestFilePath);
                }
                else
                {
                    GenerateTestFile(testManifest);
                }
            }
            return true;
        }

        #endregion

        private void GenerateTestFile(TestManifest testManifest)
        {
            var testClassTemplate = new SparqlTestClassTemplate(testManifest);
            String testClassContent = testClassTemplate.TransformText();
            File.WriteAllText(testManifest.OutputFilePath, testClassContent);
        }
    }
}
