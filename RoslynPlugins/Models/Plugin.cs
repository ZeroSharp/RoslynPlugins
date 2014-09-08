using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;

namespace RoslynPlugins.Models
{
    public class Plugin
    {
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Version { get; set; }
        [Required]
        [DataType(DataType.MultilineText)]
        public string Script { get; set; }
    }

    public class PluginDBContext : DbContext
    {
        public DbSet<Plugin> Plugins { get; set; }
    }
}