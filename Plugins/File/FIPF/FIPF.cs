using PluginInterface.Logger;
using PluginInterface.Plugin;
using Terminal.Gui;
using static PluginInterface.Interfaces.IMenu;

namespace FIPF
{
    public class FIPF : IPlugin
    {
        public static Logger Logger;
        private VoidCallback mBackCallback;
        private Window mWindow;
        private Toplevel mTop;

        private string mFilePath;
        private string mOutputPath;
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
            mTop = top;

            mWindow = new Window("FIPF")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var fileDialog = OpenDialog("Open a file", CanChooseDirectories:false);
            var dirDialog = OpenDialog("Select a directory for the output.", CanChooseFiles: false);
            
            if(fileDialog == null || dirDialog == null)
            {
                return;
            }
            mFilePath = fileDialog.FilePath.ToString();
            mOutputPath = dirDialog.DirectoryPath.ToString();
            Logger.Information(string.Format("Using Input File {0}", mFilePath));
            Logger.Information(string.Format("Using Output Directory {0}", mOutputPath));

            mTop.RemoveAll();
            mTop.Add(mWindow);
            mWindow.KeyDown += KeyDownHandler;

            Application.Run();
        }

        public OpenDialog OpenDialog(string message, bool CanChooseFiles = true, bool CanChooseDirectories = true, bool AllowsMultipleSelection = false)
        {
            mWindow.RemoveAll();
            var dialog = new OpenDialog("Open", message)
            {
                CanChooseFiles = CanChooseFiles,
                CanChooseDirectories = CanChooseDirectories,
                AllowsMultipleSelection = AllowsMultipleSelection
            };

            dialog.KeyDown += KeyDownHandler;

            Application.Run(dialog);

            if (dialog.Canceled)
            {
                mBackCallback();
                return null;
            }
            return dialog;
        }

        public void RegisterOnBack(VoidCallback oe)
        {
            mBackCallback = oe;
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
            mBackCallback();
        }
    }
}
