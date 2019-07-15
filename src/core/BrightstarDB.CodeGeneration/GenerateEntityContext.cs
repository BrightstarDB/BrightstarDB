using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BrightstarDB.CodeGeneration
{
    public class GenerateEntityContext : Task
    {
        public override bool Execute()
        {
            var loggerConfiguration = new TaskLoggerConfiguration();
            var loggerFactory = new LoggerFactory(new[] { new TaskLoggerProvider(this, loggerConfiguration) });
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
            var result = Generator.GenerateFromProjectAsync(
                entityContextLanguage,
                ProjectPath,
                EntityContextNamespace,
                EntityContextClassName,
                entityAccessibilitySelector: entityAccessibilitySelector,
                loggerFactory:loggerFactory
                ).Result;
            var resultString = result
                .Aggregate(new StringBuilder(), (sb, next) => sb.AppendLine(next.ToFullString()), x => x.ToString());

            File.WriteAllText(EntityContextFileName, resultString);
            return true;
        }

        [Required]
        public string ProjectPath { get; set; }

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

    public class TaskLoggerProvider : ILoggerProvider
    {
        private readonly TaskLoggingHelper _loggingHelper;
        private readonly TaskLoggerConfiguration _configuration;

        public TaskLoggerProvider(ITask task, TaskLoggerConfiguration configuration)
        {
            _configuration = configuration;
            _loggingHelper = new TaskLoggingHelper(task);
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TaskLogger(_loggingHelper, categoryName, _configuration);
        }
    }

    public class TaskLoggerConfiguration
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;
        public int EventId { get; set; } = 0;
    }

    public class TaskLogger : ILogger
    {
        private readonly TaskLoggingHelper _loggingHelper;
        private readonly string _categoryName;
        private readonly TaskLoggerConfiguration _config;

        public TaskLogger(TaskLoggingHelper loggingHelper, string categoryName, TaskLoggerConfiguration config)
        {
            _config = config;
            _loggingHelper = loggingHelper;
            _categoryName = categoryName;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel < _config.LogLevel) return;
            switch (logLevel)
            {
                case LogLevel.Debug:
                case LogLevel.Trace:
                    _loggingHelper.LogMessage(MessageImportance.Low, $"{logLevel.ToString()} - ${eventId} - {_categoryName} - {formatter(state, exception)}");
                    break;
                case LogLevel.Information:
                case LogLevel.Warning:
                    _loggingHelper.LogMessage(MessageImportance.Normal, $"{logLevel.ToString()} - ${eventId} - {_categoryName} - {formatter(state, exception)}");
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    _loggingHelper.LogMessage(MessageImportance.High, $"{logLevel.ToString()} - ${eventId} - {_categoryName} - {formatter(state, exception)}");
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _config.LogLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    public class TaskLoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}
