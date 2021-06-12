using PluginInterface.Common.File;
using PluginInterface.Logger;
using PluginInterface.Plugin;
using System.IO;
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
            // load file headers
            if (FileHeaders.Headers == null)
            {
                new FileHeaders();
            }

            Logger.Instance.Information("Entered FIPF");
            mTop = top;
            mTop.RemoveAll();
            mWindow = new Window("FIPF")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var fileDialog = OpenDialog("Open a file", CanChooseDirectories: false);
            var dirDialog = OpenDialog("Select a directory for the output.", CanChooseFiles: false);

            if (fileDialog == null || dirDialog == null)
            {
                return;
            }
            mFilePath = fileDialog.FilePath.ToString();
            mOutputPath = dirDialog.FilePath.ToString();
            Logger.Information(string.Format("Using Input File {0}", mFilePath));
            Logger.Information(string.Format("Using Output Directory {0}", mOutputPath));

            mTop.RemoveAll();
            mTop.Add(mWindow);
            mWindow.KeyDown += KeyDownHandler;

            FileInfo file = new FileInfo(mFilePath);

            foreach (var header in FileHeaders.Headers.File)
            {
                var headerSearch = new BoyerMooreBinarySearch(FileHeaders.ByteArrayFromString(header.Header));
                var footerSearch = new BoyerMooreBinarySearch(FileHeaders.ByteArrayFromString(header.Footer));
                var headerIndexes = headerSearch.GetMatchIndexes(file);
                var footerIndexes = footerSearch.GetMatchIndexes(file);
                string dirPath = Path.Combine(mOutputPath, header.Name);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                for (int i = 0; i < headerIndexes.Count && i < footerIndexes.Count; i++)
                {
                    long start = headerIndexes[i];
                    long end = footerIndexes[i];
                    using FileStream stream = file.OpenRead();
                    var fileStream = System.IO.File.Create(Path.Combine(dirPath, Path.GetRandomFileName() + "." + header.Name));
                    Logger.Information(string.Format("Using start = {0} end = {1} for file {2}", start, end, fileStream.Name));
                    stream.Seek(start, SeekOrigin.Begin);
                    long fileBytes = end - start;
                    byte[] bytearray = new byte[fileBytes];
                    stream.Read(bytearray, 0, (int)fileBytes);
                    fileStream.Write(bytearray, 0, (int)fileBytes);
                    fileStream.Close();
                }
            }

            Application.Run();
        }

        private OpenDialog OpenDialog(string message, bool CanChooseFiles = true, bool CanChooseDirectories = true, bool AllowsMultipleSelection = false)
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
