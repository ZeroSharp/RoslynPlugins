using System;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using RoslynPlugins.Controllers;
using RoslynPlugins.Plugins;

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

    public class PluginModule : NinjectModule
    {
        private IGenerator CreateGenerator(IContext context)
        {
            PluginLocator pluginLocator = context.Kernel.Get<PluginLocator>();
            Type roslynPluginType = pluginLocator.Locate<IGenerator>();
            return (IGenerator)context.Kernel.Get(roslynPluginType ?? typeof(HelloWorldGenerator));
        }

        public override void Load()
        {
            Bind<IGenerator>().ToMethod(context => CreateGenerator(context));

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

            Bind<IPluginProvider>().To<PluginProvider>();
            Bind<PluginAssemblyCache>().ToSelf().InSingletonScope();
            Bind<IAssemblyReferenceCollector>().To<AssemblyReferenceCollector>();
        }
    }
}