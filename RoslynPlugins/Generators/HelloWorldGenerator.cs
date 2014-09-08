using System;

namespace RoslynPlugins.Controllers
{
    public class HelloWorldGenerator : IGenerator
    {
        public string Generate()
        {
            return "Hello World!";
        }
    }
}
