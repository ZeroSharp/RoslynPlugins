using System;
using System.Linq;
using Ninject.Extensions.Conventions;
using Ninject.Modules;
using RoslynPlugins.Controllers;
using Ninject.Extensions.Conventions.Syntax;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Extensions.Conventions.BindingBuilder;
using Ninject.Syntax;
using System.Collections.Generic;
using System.Reflection;
using Ninject;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.Serialization;
using Ninject.Activation;
using RoslynPlugins.Models;


namespace RoslynPlugins.App_Start
{
    //public static class ConventionSyntaxExtensions
    //{
    //    public static IConfigureSyntax BindToPluginOrDefaultInterfaces(this IJoinFilterWhereExcludeIncludeBindSyntax syntax)
    //    {
    //        return syntax.BindWith(new DefaultInterfacesBindingGenerator(new BindableTypeSelector(), new PluginOrDefaultBindingCreator()));
    //    }
    //}

    ///// <summary>
    ///// Returns a Ninject binding to a method which returns the plugin type if one exists, otherwise returns the default type.
    ///// </summary>
    //public class PluginOrDefaultBindingCreator : IBindingCreator
    //{
    //    public IEnumerable<IBindingWhenInNamedWithOrOnSyntax<object>> CreateBindings(IBindingRoot bindingRoot, IEnumerable<Type> serviceTypes, Type implementationType)
    //    {
    //        if (bindingRoot == null)
    //        {
    //            throw new ArgumentNullException("bindingRoot");
    //        }

    //        return !serviceTypes.Any()
    //         ? Enumerable.Empty<IBindingWhenInNamedWithOrOnSyntax<object>>()
    //         : new[] { bindingRoot.Bind(serviceTypes.ToArray()).ToMethod(context => context.Kernel.Get(context.Kernel.Get<PluginLocator>().Locate(serviceTypes) ?? implementationType)) };
    //    }
    //}

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

    public class PluginLocator
    {
        public PluginLocator(PluginAssemblyDictionary pluginAssemblyDictionary)
        {
            if (pluginAssemblyDictionary == null)
                throw new ArgumentNullException("pluginProvider");
            
            _PluginAssemblyDictionary = pluginAssemblyDictionary;
        }

        private readonly PluginAssemblyDictionary _PluginAssemblyDictionary;

        public Type Locate<T>()
        {
            return Locate(new [] { typeof(T) });
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

    public static class PluginExtensions
    {
        public class PluginScriptContainer : IPluginScriptContainer
        {
            public string Name { get; set; }
            public string Script { get; set; }
            public string Version { get; set; }
        }

        public static IPluginScriptContainer ToPluginScriptContainer(this Plugin p)
        {
            return new PluginScriptContainer()
            {
                Name = p.Name,
                Script = p.Script,
                Version = p.Version
            };
        }
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

        public IEnumerable<IPluginScriptContainer> GetPlugins()
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

    /// <summary>
    /// This class maintains a list of runtime-compiled in memory assemblies loaded from the plugins
    /// available via the provider. It is a singleton class.
    /// </summary>
    public class PluginAssemblyDictionary
    {
        public PluginAssemblyDictionary(IPluginProvider pluginProvider, PluginLoader pluginLoader)
        {
            if (pluginProvider == null)
                throw new ArgumentNullException("pluginProvider");
            _PluginProvider = pluginProvider;

            if (pluginLoader == null)
                throw new ArgumentNullException("pluginLoader");
            _PluginLoader = pluginLoader;
        }

        private class DictionaryEntry
        {
            public string Name { get; set; }
            public Version Version { get; set; }
            public Assembly Assembly { get; set; }
        }

        private readonly IPluginProvider _PluginProvider;
        private readonly PluginLoader _PluginLoader;

        private List<DictionaryEntry> _Dictionary = new List<DictionaryEntry>();

        private void Add(string name, string version, Assembly assembly)
        {
            var dictionaryEntry =
                new DictionaryEntry()
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

    public interface IPluginProvider
    {
        IEnumerable<IPluginScriptContainer> GetPlugins();
    }

    public interface IPluginScriptContainer
    {
        string Name { get; }
        string Script { get; }
        string Version { get; }
    }

    public class PluginLoader
    {
        public PluginLoader(PluginSourceCompiler pluginSourceCompiler)
        {
            if (pluginSourceCompiler == null)
                throw new ArgumentNullException("pluginSourceCompiler");

            _PluginSourceCompiler = pluginSourceCompiler;
        }

        private readonly PluginSourceCompiler _PluginSourceCompiler;

        public Assembly Load(IPluginScriptContainer plugin)
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

    public class PluginSourceCompiler
    {
        public PluginSourceCompiler(IAssemblyReferenceCollector assemblyReferenceCollector)
        {
            if (assemblyReferenceCollector == null)
                throw new ArgumentNullException("assemblyReferenceCollector");
            
            _AssemblyReferenceCollector = assemblyReferenceCollector;
        }

        private readonly IAssemblyReferenceCollector _AssemblyReferenceCollector;

        private IEnumerable<Diagnostic> _Diagnostics = Enumerable.Empty<Diagnostic>();

        public IEnumerable<Diagnostic> Errors
        {
            get
            {
                return _Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error);
            }
        }

        public IEnumerable<Diagnostic> Warnings
        {
            get
            {
                return _Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Warning);
            }
        }

        private string GetOutputAssemblyName(string name)
        {
            return String.Format("NetMWC.Plugin.{0}", name);
        }

        /// <summary>
        /// Compiles source code at runtime into an assembly. The assembly will automatically include all
        /// the same assembly references as NetMWC, so you can call any function which is available from
        /// within NetMWC. Compilation errors and warnings can be obtained from the Errors and Warnings properties.
        /// </summary>
        /// <param name="sourceCode">Source code such as the contents of AbnAmroGenerator.cs</param>
        /// <returns>The compiled assembly in memory. If there were errors, it will return null.</returns>
        public Assembly Compile(string name, string script)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (script == null)
                throw new ArgumentNullException("script");

            string outputAssemblyName = GetOutputAssemblyName(name);

            var defaultImplementationAssembly = typeof(HelloWorldGenerator).Assembly;
            var assemblyReferences = _AssemblyReferenceCollector.CollectMetadataReferences(defaultImplementationAssembly);

            // Parse the script to a SyntaxTree
            var syntaxTree = CSharpSyntaxTree.ParseText(script);

            // Compile the SyntaxTree to an in memory assembly
            var compilation = CSharpCompilation.Create(outputAssemblyName,
                new[] { syntaxTree },
                assemblyReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var outputStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    // Emit assembly to streams. Throw an exception if there are any compilation errors
                    var result = compilation.Emit(outputStream, pdbStream: pdbStream);

                    // Populate the _diagnostics property in order to read Errors and Warnings
                    _Diagnostics = result.Diagnostics;

                    if (result.Success)
                    {
                        return Assembly.Load(outputStream.ToArray(), pdbStream.ToArray());
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }

    [Serializable]
    public class PluginCompilationException : Exception
    {
        // constructors...
        #region PluginCompilationException()
        /// <summary>
        /// Constructs a new PluginCompilationException.
        /// </summary>
        public PluginCompilationException() { }
        #endregion
        #region PluginCompilationException(string message)
        /// <summary>
        /// Constructs a new PluginCompilationException.
        /// </summary>
        /// <param name="message">The exception message</param>
        public PluginCompilationException(string message) : base(message) { }
        #endregion
        #region PluginCompilationException(string message, Exception innerException)
        /// <summary>
        /// Constructs a new PluginCompilationException.
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public PluginCompilationException(string message, Exception innerException) : base(message, innerException) { }
        #endregion
        #region PluginCompilationException(SerializationInfo info, StreamingContext context)
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        protected PluginCompilationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }

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

    public class PluginModule : NinjectModule
    {
        private IGenerator CreateInstance(IContext context)
        {
            PluginLocator pluginLocator = context.Kernel.Get<PluginLocator>();
            Type roslynPluginType = pluginLocator.Locate<IGenerator>();
            return (IGenerator)context.Kernel.Get(roslynPluginType ?? typeof(HelloWorldGenerator));
        }

        public override void Load()
        {
            Bind<IGenerator>().ToMethod(context => CreateInstance(context));

            /// If you have a lot of IGenerator subclasses, you can use Ninject's
            /// convention based module.
            /// 
            ///// For each Generator, bind to IGenerator. 
            ///// For example, Bind<IGenerator>.To<SomeGenerator>();
            ///// 
            ///// Also, if a candidate class exists in a plugin assembly, use it instead of the default one.
            //Kernel.Bind(scanner => scanner
            //    .FromThisAssembly()
            //    .SelectAllClasses()
            //    .InheritedFrom(typeof(IGenerator))
            //    .BindToPluginOrDefaultInterfaces()); //This is a custom extension method

            //Bind<IPaymentInstructionGeneratorFactory>().To<PaymentInstructionGeneratorFactory>();
            Bind<IPluginProvider>().To<PluginProvider>();
            Bind<PluginAssemblyDictionary>().ToSelf().InSingletonScope();
            Bind<IAssemblyReferenceCollector>().To<AssemblyReferenceCollector>();
        }
    }
}