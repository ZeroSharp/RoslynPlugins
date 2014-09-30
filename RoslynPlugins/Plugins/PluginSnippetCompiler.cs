using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynPlugins.Controllers;

namespace RoslynPlugins.Plugins
{
    public class PluginSnippetCompiler
    {
        public PluginSnippetCompiler(IAssemblyReferenceCollector assemblyReferenceCollector)
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
        /// the same assembly references as the main RoslynPlugins assembly, so you can call any function which is
        /// available from within RoslynPlugins. Compilation errors and warnings can be obtained from the Errors and
        /// Warnings properties.
        /// </summary>
        /// <param name="script">Source code such as the contents of HelloWorldGenerator.cs</param>
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
}
