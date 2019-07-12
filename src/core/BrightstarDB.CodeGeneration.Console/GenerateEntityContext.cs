using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;

namespace BrightstarDB.CodeGeneration.Console
{
    public class GenerateEntityContext : Task
    {
        public override bool Execute()
        {
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
