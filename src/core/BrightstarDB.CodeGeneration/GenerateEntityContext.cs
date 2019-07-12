using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;

namespace BrightstarDB.CodeGeneration
{
    public class GenerateEntityContext : Task
    {
        public override bool Execute()
        {
            var searchPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(GenerateEntityContext)).Location);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblySearchPath = Path.Combine(searchPath, args.Name.Split(',')[0]) + ".dll";
                if (File.Exists(assemblySearchPath))
                {
                    return Assembly.LoadFrom(assemblySearchPath);
                }
                return null;
            };
            Log.LogMessage("BrightstarDB Entity Context Code Generation");
            var entityContextLanguage = GetEntityContextLanguage();
            var entityAccessibilitySelector = EntityClassesInternal
                ? (Func<INamedTypeSymbol, Accessibility>)Generator.InteralyEntityAccessibilitySelector
                : Generator.DefaultEntityAccessibilitySelector;
            var result = Generator.GenerateAsync(
                entityContextLanguage,
                SolutionPath,
                EntityContextNamespace,
                EntityContextClassName,
                entityAccessibilitySelector: entityAccessibilitySelector
                ).Result;
            var resultString = result
                .Aggregate(new StringBuilder(), (sb, next) => sb.AppendLine(next.ToFullString()), x => x.ToString());

            File.WriteAllText(EntityContextFileName, resultString);
            return true;
        }

        [Required]
        public string SolutionPath { get; set; }

        [Required]
        public string EntityContextNamespace { get; set; }

        public string EntityContextFileName { get; set; } = "EntityContext.cs";
        public string EntityContextClassName { get; set; } = "EntityContext";
        public bool EntityClassesInternal { get; set; }
        

        private Language GetEntityContextLanguage()
        {
            if (!string.IsNullOrEmpty(EntityContextFileName) &&
                EntityContextFileName.EndsWith(".vb", StringComparison.InvariantCultureIgnoreCase))
            {
                return Language.VisualBasic;
            }

            return Language.CSharp;
        }
    }
}
