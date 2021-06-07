using PluginInterface.Logger;
using SawMill.App.Menu;
using SawMill.App.Plugin;

namespace SawMill
{
    class Program
    {

        static void Main(string[] args)
        {
            Logger.Instance.Information("Starting");
            new MainMenu(new PluginManager().plugins).StartMenu();

        }

    }
}
