using System;
using System.Linq;
using System.Reflection;

namespace RoslynPlugins.Plugins
{
    public class PluginLoader
    {
        public PluginLoader(PluginSnippetCompiler pluginSourceCompiler)
        {
            if (pluginSourceCompiler == null)
                throw new ArgumentNullException("pluginSourceCompiler");

            _PluginSourceCompiler = pluginSourceCompiler;
        }

        private readonly PluginSnippetCompiler _PluginSourceCompiler;

        public Assembly Load(IPluginSnippetContainer plugin)
        {
            var name = plugin.Name;
            var script = plugin.Script;

            var assembly = _PluginSourceCompiler.Compile(name, script);
            if (assembly == null)
            {
                var errors = String.Join(Environment.NewLine, _PluginSourceCompiler.Errors);
                throw new PluginCompilationException(String.Format("Failed to load the {0} plugin. The following compilation errors occurred:\n\n{1}", plugin.Name, errors));
            }
            return assembly;
        }
    }
}
