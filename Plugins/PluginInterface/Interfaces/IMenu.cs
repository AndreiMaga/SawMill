using Terminal.Gui;

namespace PluginInterface.Interfaces
{
    public interface IMenu
    {
        public delegate void VoidCallback();
        public string GetCategory();
        public string GetName();
        public string GetDescription();

        public void OnEnter(Toplevel top);

        public void SetLogger(Logger.Logger logger);

        public void KeyDownHandler(View.KeyEventEventArgs e);
        public void RegisterOnBack(VoidCallback oe);
        public void OnBack();
    }
}
