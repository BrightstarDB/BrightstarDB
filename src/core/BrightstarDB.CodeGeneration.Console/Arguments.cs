namespace BrightstarDB.CodeGeneration.Console
{
    using PowerArgs;

    public sealed class Arguments
    {
        [ArgRequired]
        [ArgExistingFile]
        [ArgDescription("The solution file to examine for potential entities to be generated.")]
        [ArgPosition(0)]
        public string SolutionFile
        {
            get;
            set;
        }

        [ArgRequired]
        [ArgDescription("The namespace for the generated entity context.")]
        [ArgPosition(1)]
        public string ContextNamespace
        {
            get;
            set;
        }

        [ArgRequired]
        [ArgDescription("The output file containing the generated code.")]
        [ArgPosition(2)]
        public string OutputFile
        {
            get;
            set;
        }

        [ArgDescription("The class name for the generated entity context.")]
        [ArgDefaultValue("EntityContext")]
        [ArgShortcut("CN")]
        public string ContextName
        {
            get;
            set;
        }

        [ArgDescription("Optionally force the language in which to generate mocks.")]
        public Language? Language
        {
            get;
            set;
        }
    }
}