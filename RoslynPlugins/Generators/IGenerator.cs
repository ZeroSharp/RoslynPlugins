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
}
