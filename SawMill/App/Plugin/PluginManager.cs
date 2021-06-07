using PluginInterface.Logger;
using PluginInterface.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SawMill.App.Plugin
{
    class PluginManager
    {
        private readonly string mPluginFolder = "Plugins";

        public List<Assembly> assemblies = new();
        public Dictionary<string, List<IPlugin>> plugins = new();

        public PluginManager()
        {
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            if (!Directory.Exists(mPluginFolder))
            {
                return;
            }
            foreach (var directory in new DirectoryInfo(mPluginFolder).GetDirectories())
            {
                // search for the dll file
                var file = directory.GetFiles().Where(dll => dll.Extension.Equals(".dll")).FirstOrDefault();
                if (file == null)
                    continue;
                var DLL = Assembly.LoadFile(file.FullName);
                assemblies.Add(DLL);
            }

            FindIPlugins();
        }

        private void FindIPlugins()
        {
            foreach (var assemblie in assemblies)
            {
                foreach (var type in assemblie.GetTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type))
                    {
                        IPlugin instance = (IPlugin)Activator.CreateInstance(type);
                        instance.SetLogger(Logger.Instance);
                        string category = instance.GetCategory();
                        if (plugins.ContainsKey(category))
                        {
                            plugins[category].Add(instance);
                        }
                        else
                        {
                            List<IPlugin> list = new();
                            list.Add(instance);
                            plugins.Add(category, list);
                        }
                    }
                }
            }
            Logger.Instance.Information(string.Format("Loaded {0} plugins.", plugins.Count));
        }
    }
}
