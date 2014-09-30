using System;
using System.Collections.Generic;
using System.Linq;
using RoslynPlugins.Controllers;
using RoslynPlugins.Models;

namespace RoslynPlugins.Plugins
{
    public interface IPluginProvider
    {
        IEnumerable<IPluginSnippetContainer> GetPlugins();
    }

    public class PluginProvider : IPluginProvider
    {
        public PluginProvider(PluginDBContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            _Context = context;
        }

        private readonly PluginDBContext _Context;

        public IEnumerable<IPluginSnippetContainer> GetPlugins()
        {
            var currentVersion = typeof(HelloWorldGenerator).Assembly.GetName().Version;

            /// only consider plugins with version numbers greater than the 
            /// currently shipped version.
            return _Context.Plugins
                .ToList() // eager load so we can compare the version number adequately
                .Where(p => new Version(p.Version) > currentVersion)
                .Select(p => p.ToPluginScriptContainer())
                .ToArray();
        }
    }
}
