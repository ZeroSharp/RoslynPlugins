using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RoslynPlugins.Plugins
{
    public class AssemblyExplorer
    {
        public static IEnumerable<Type> GetImplementingClasses<T>()
        {
            return typeof(T)
                .Assembly
                .GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
        }

        public static IEnumerable<Type> GetImplementingClasses(IEnumerable<Assembly> assemblies, IEnumerable<Type> interfaces)
        {
            return assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => interfaces.All(i => i.IsAssignableFrom(t)) && t.IsClass && !t.IsAbstract);
        }
    }
}
