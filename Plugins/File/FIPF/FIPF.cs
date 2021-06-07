using PluginInterface.Logger;
using PluginInterface.Plugin;
using Terminal.Gui;
using static PluginInterface.Interfaces.IMenu;

namespace FIPF
{
    public class FIPF : IPlugin
    {
        private VoidCallback backCallback;
        public static Logger Logger;

        public string GetCategory() => "File";

        public string GetDescription() => "A file carving algorithm.";
        public string GetName() => "Fast In-Place File carving";

        public void SetLogger(Logger logger)
        {
            Logger = logger;
        }

        public void OnEnter(Toplevel top)
        {
            Logger.Instance.Information("Entered FIPF");

            top.RemoveAll();
            var window = new Window("FIPF")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(window);
            top.KeyDown += KeyDownHandler;
            Application.Run();
        }

        public void RegisterOnBack(VoidCallback oe)
        {
            backCallback = oe;
        }

        public void KeyDownHandler(View.KeyEventEventArgs e)
        {
            if (e.KeyEvent.Key == Key.Esc)
            {
                OnBack();
            }
        }

        public void OnBack()
        {
            backCallback();
        }
    }
}
