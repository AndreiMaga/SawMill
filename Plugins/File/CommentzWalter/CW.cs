using PluginInterface.Common.File;
using PluginInterface.Logger;
using PluginInterface.Plugin;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using static CommentzWalter.CommentzWalter;
using static PluginInterface.Interfaces.IMenu;

namespace CommentzWalter
{
    public class CW : IPlugin
    {
        private VoidCallback mBackCallback;
        private Window mWindow;
        private Toplevel mTop;

        private string mFilePath;
        private string mOutputPath;

        public Logger Logger { get; private set; }

        public string GetCategory()
        {
            return "File";
        }

        public string GetDescription()
        {
            return "File carving with Commentz-Walter pattern search";
        }

        public string GetName()
        {
            return "Commentz-Walter";
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

        public void OnEnter(Toplevel top)
        {
            // load file headers
            if (FileHeaders.Headers == null)
            {
                new FileHeaders();
            }

            Logger.Instance.Information("Entered Commentz Walter");
            mTop = top;
            mTop.RemoveAll();
            mWindow = new Window("Commentz Walter")
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

            var cw = new CommentzWalter(file, Logger);

            List<Task> taskList = new();

            foreach (var kvp in cw.FinalResults)
            {
                string fileType = kvp.Key;
                List<Result> headerResults = kvp.Value.Where(v => v.Type == "Header").ToList();
                List<Result> footerResults = kvp.Value.Where(v => v.Type == "Footer").ToList();

                Task t = new Task(() =>
                {
                    SaveAsync(file, fileType, headerResults, footerResults);
                });
                t.Start();
                taskList.Add(t);
            }

            Task.WaitAll(taskList.ToArray());

            Application.Run();
        }

        private void SaveAsync(FileInfo file, string fileType, List<Result> headerResults, List<Result> footerResults)
        {
            using FileStream stream = file.OpenRead();
            string dirPath = Path.Combine(mOutputPath, fileType);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            for (int i = 0; i < headerResults.Count && i < footerResults.Count; i++)
            {
                long start = headerResults[i].Index;
                long end = footerResults[i].Index;
                var fileStream = System.IO.File.Create(Path.Combine(dirPath, Path.GetRandomFileName() + "." + fileType));
                Logger.Information(string.Format("Using start = {0} end = {1} for file {2}", start, end, fileStream.Name));
                stream.Seek(start, SeekOrigin.Begin);
                long fileBytes = end - start;
                byte[] bytearray = new byte[fileBytes];
                stream.Read(bytearray, 0, (int)fileBytes);
                fileStream.Write(bytearray, 0, (int)fileBytes);
                fileStream.Close();
            }
        }

        private OpenDialog OpenDialog(string message, bool CanChooseFiles = true, bool CanChooseDirectories = true, bool AllowsMultipleSelection = false)
        {
            mWindow.RemoveAll();
            var dialog = new OpenDialog("Open", message)
            {
                CanChooseFiles = CanChooseFiles,
                CanChooseDirectories = CanChooseDirectories,
                AllowsMultipleSelection = AllowsMultipleSelection,
                DirectoryPath = @"C:\Users\Andrei\Desktop\Repos\SawMill\tests\img"
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

        public void SetLogger(Logger logger)
        {
            Logger = logger;
        }
    }
}
