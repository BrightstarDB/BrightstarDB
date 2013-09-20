using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BrightstarDB.Portable.Adaptation
{
    internal class ProbingAdapterResolver : IAdapterResolver
    {
        private readonly string[] _platformNames;
        private readonly Func<string, Assembly> _assemblyLoader;
        private readonly object _lock = new object();
        private Dictionary<Type, object> _adapters = new Dictionary<Type, object>();
        private Assembly _assembly;

        public ProbingAdapterResolver(string[] platformNames) :
            this(Assembly.Load, platformNames)
        {
            
        }

        public ProbingAdapterResolver(Func<string, Assembly> assemblyLoader, string[] platformNames)
        {
            _platformNames = platformNames;
            _assemblyLoader = assemblyLoader;
        }

        public object Resolve(Type type)
        {
            lock (_lock)
            {
                object instance;
                if (!_adapters.TryGetValue(type, out instance))
                {
                    Assembly assembly = GetPlatformSpecificAssembly();
                    instance = ResolveAdapter(assembly, type);
                    _adapters.Add(type, instance);
                }
                return instance;
            }
        }
        private static object ResolveAdapter(Assembly assembly, Type interfaceType)
        {
            string typeName = MakeAdapaterTypeName(interfaceType);
            Type type = assembly.GetType(typeName, throwOnError: false);
            if (type != null) return Activator.CreateInstance(type);
            return type;
        }

        private static string MakeAdapaterTypeName(Type interfaceType)
        {
            return interfaceType.Namespace + "." + interfaceType.Name.Substring(1);
        }

        private Assembly GetPlatformSpecificAssembly()
        {
            if (_assembly == null)
            {
                _assembly = ProbeForPlatformSpecificAssembly();
                if (_assembly == null)
                {
                    throw new InvalidOperationException("Platform-specific assembly not found");
                }
            }
            return _assembly;
        }

        private Assembly ProbeForPlatformSpecificAssembly()
        {
            foreach (var platformName in _platformNames)
            {
                Assembly assembly = ProbeForPlatformSpecificAssembly(platformName);
                if (assembly != null) return assembly;
            }
            return null;
        }

        private Assembly ProbeForPlatformSpecificAssembly(string platformName)
        {
            var assemblyName = new AssemblyName(GetType().Assembly.FullName);
            assemblyName.Name = "BrightstarDB.Portable." + platformName;
            try
            {
                return _assemblyLoader(assemblyName.FullName);
            }
            catch (FileNotFoundException)
            {
                
            }
            return null;
        }
    }
}