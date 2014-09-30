using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynPlugins.Plugins
{
    public class PluginLocator
    {
        public PluginLocator(PluginAssemblyCache pluginAssemblyDictionary)
        {
            if (pluginAssemblyDictionary == null)
                throw new ArgumentNullException("pluginProvider");

            _PluginAssemblyDictionary = pluginAssemblyDictionary;
        }

        private readonly PluginAssemblyCache _PluginAssemblyDictionary;

        public Type Locate<T>()
        {
            return Locate(new[] { typeof(T) });
        }

        protected Type Locate(IEnumerable<Type> serviceTypes)
        {
            var implementingClasses = AssemblyExplorer.GetImplementingClasses(_PluginAssemblyDictionary.GetAssemblies(), serviceTypes);

            if (implementingClasses.Any())
            {
                if (implementingClasses.Count() > 1)
                    throw new Exception("More than one plugin class found which implements " + String.Join(" + ", serviceTypes.Select(t => t.ToString())));
                else
                    return implementingClasses.Single();
            }
            return null;
        }
    }
}
