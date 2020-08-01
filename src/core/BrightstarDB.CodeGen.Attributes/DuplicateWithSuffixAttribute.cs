using System;
using System.Diagnostics;
using CodeGeneration.Roslyn;

namespace BrightstarDB.CodeGen
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [CodeGenerationAttribute("BrightstarDB.CodeGen.Generators.DuplicateWithSuffixGenerator, BrightstarDB.CodeGen.Generators")]
    [Conditional("CodeGeneration")]
    public class DuplicateWithSuffixAttribute : Attribute
    {
        public DuplicateWithSuffixAttribute(string suffix)
        {
            Suffix = suffix;
        }

        public string Suffix { get; }
    }
}
