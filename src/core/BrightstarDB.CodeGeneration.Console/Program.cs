namespace BrightstarDB.CodeGeneration.Console
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using PowerArgs;

    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var arguments = Args.Parse<Arguments>(args);
                var language = arguments.Language.GetValueOrDefault(DetermineLanguageByOutputFileName(arguments.OutputFile));
                var result = Generator.GenerateAsync(
                    language,
                    arguments.SolutionFile,
                    arguments.ContextNamespace,
                    arguments.ContextName).Result;
                var resultString = result
                    .Aggregate(new StringBuilder(), (sb, next) => sb.AppendLine(next.ToFullString()), x => x.ToString());

                File.WriteAllText(arguments.OutputFile, resultString);
                return 0;
            }
            catch (ArgException ex)
            {
                WriteLine(OutputType.Error, ex.Message);
                WriteLine();
                WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>().ToNormalString());
                return -1;
            }
            catch (Exception ex)
            {
                WriteLine(OutputType.Error, "Failed to generate code: {0}", ex);
                return -1;
            }
        }

        private static void WriteLine()
        {
            WriteLine(OutputType.Normal, "");
        }

        private static void WriteLine(string format, params object[] args)
        {
            WriteLine(OutputType.Normal, format, args);
        }

        private static void WriteLine(OutputType outputType, string format, params object[] args)
        {
            var previousColor = default(ConsoleColor);

            if (outputType == OutputType.Error)
            {
                previousColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine(format, args);

            if (outputType == OutputType.Error)
            {
                Console.ForegroundColor = previousColor;
            }
        }

        private static Language DetermineLanguageByOutputFileName(string fileName)
        {
            switch (Path.GetExtension(fileName))
            {
                case ".vb":
                    return Language.VisualBasic;
                default:
                    return Language.CSharp;
            }
        }

        private enum OutputType
        {
            Normal,
            Error
        }
    }
}