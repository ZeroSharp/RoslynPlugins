using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace RoslynPlugins.Plugins
{
    public interface IAssemblyReferenceCollector
    {
        IEnumerable<MetadataReference> CollectMetadataReferences(Assembly assembly);
    }

    public class AssemblyReferenceCollector : IAssemblyReferenceCollector
    {
        public IEnumerable<MetadataReference> CollectMetadataReferences(Assembly assembly)
        {
            var referencedAssemblyNames = assembly.GetReferencedAssemblies();

            var references = new List<MetadataReference>();
            foreach (AssemblyName assemblyName in referencedAssemblyNames)
            {
                var loadedAssembly = Assembly.Load(assemblyName);
                references
                    .Add(new MetadataFileReference(loadedAssembly.Location));
            }

            references
                .Add(new MetadataFileReference(assembly.Location)); // add a reference to 'self', i.e., NetMWC

            return references;
        }
    }
}
