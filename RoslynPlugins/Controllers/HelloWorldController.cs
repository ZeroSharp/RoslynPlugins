using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RoslynPlugins.Controllers
{
    public interface IGenerator
    {
        string Generate();
    }

    public class HelloWorldGenerator : IGenerator
    {
        public string Generate()
        {
            return "Hello World!";
        }
    }

    public class HelloWorldController : Controller
    {
        public HelloWorldController(IGenerator generator)
        {
            if (generator == null)
                throw new ArgumentNullException("generator");

            _Generator = generator;
        }

        public IGenerator _Generator { get; private set; }

        //
        // GET: /HelloWorld/
        public ActionResult Index()
        {
            string output = _Generator.Generate();
            string sanitizedOutput = HttpUtility.HtmlEncode(output);

            ViewBag.GeneratorOutput = sanitizedOutput;
            return View();
        }
    }
}