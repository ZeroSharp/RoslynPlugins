using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RoslynPlugins.Plugins
{
    /// <summary>
    /// This class maintains a list of runtime-compiled in memory assemblies loaded from the plugins
    /// available via the provider. It is a singleton class.
    /// </summary>
    public class PluginAssemblyCache
    {
        public PluginAssemblyCache(IPluginProvider pluginProvider, PluginLoader pluginLoader)
        {
            if (pluginProvider == null)
                throw new ArgumentNullException("pluginProvider");
            _PluginProvider = pluginProvider;

            if (pluginLoader == null)
                throw new ArgumentNullException("pluginLoader");
            _PluginLoader = pluginLoader;
        }

        private class CacheEntry
        {
            public string Name { get; set; }
            public Version Version { get; set; }
            public Assembly Assembly { get; set; }
        }

        private readonly IPluginProvider _PluginProvider;
        private readonly PluginLoader _PluginLoader;

        private List<CacheEntry> _Dictionary = new List<CacheEntry>();

        private void Add(string name, string version, Assembly assembly)
        {
            var dictionaryEntry =
                new CacheEntry()
                {
                    Name = name,
                    Version = new Version(version),
                    Assembly = assembly
                };
            _Dictionary.Add(dictionaryEntry);
        }

        private void RefreshDictionary()
        {
            var pluginScriptContainers = _PluginProvider.GetPlugins();

            // Add a new assembly for any new or updated plugin
            foreach (var pluginScriptContainer in pluginScriptContainers)
            {
                var name = pluginScriptContainer.Name;
                var version = pluginScriptContainer.Version;
                if (!_Dictionary.Any(a => a.Name == name && a.Version == new Version(version)))
                {
                    var assembly = _PluginLoader.Load(pluginScriptContainer);
                    Add(name, version, assembly);
                }
            }

            // Remove any assemblies which we no longer have a plugin for.
            _Dictionary
                .RemoveAll(dictionaryEntry =>
                    !pluginScriptContainers
                        .Select(plugin => plugin.Name)
                        .Contains(dictionaryEntry.Name));
        }

        public IEnumerable<Assembly> GetAssemblies()
        {
            RefreshDictionary();

            // Return only the assemblies with the highest version numbers
            return _Dictionary
                .GroupBy(d => d.Name)
                .Select(g => g
                        .OrderByDescending(d => d.Version)
                        .First()
                        .Assembly);
        }
    }
}
