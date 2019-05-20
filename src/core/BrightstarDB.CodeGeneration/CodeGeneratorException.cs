using System;

namespace BrightstarDB.CodeGeneration
{
    public class CodeGeneratorException : Exception
    {
        public CodeGeneratorException(string msg):base(msg)
        {
        }
    }
}