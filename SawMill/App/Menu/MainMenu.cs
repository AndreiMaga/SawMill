using PluginInterface.Plugin;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace SawMill.App.Menu
{
    class MainMenu
    {
        private Toplevel mTop;
        private FrameView mCategoriesView;
        private FrameView mPluginsView;
        private ListView mCategoriesListView;
        private ListView mPluginsListView;
        private List<string> mListOfCategories = new();
        private readonly Dictionary<string, List<IPlugin>> mPlugins;
        private int mSelectedCategory;
        private int mSelectedAlgo;
        public MainMenu(Dictionary<string, List<IPlugin>> plugins)
        {
            mPlugins = plugins;
        }

        public void StartMenu()
        {
            Application.UseSystemConsole = false;
            Application.Init();
            Application.HeightAsBuffer = false;
            Application.AlwaysSetPosition = true;
            mTop = Application.Top;
            AddMenu();
            ShowMenu();
        }

        public void AddMenu()
        {
            mCategoriesView = new FrameView("Categories")
            {
                X = 0,
                Y = 1,
                Width = 25,
                Height = Dim.Fill(1),
                CanFocus = false,
                Shortcut = Key.CtrlMask | Key.C
            };
            mCategoriesView.Title = $"{mCategoriesView.Title} ({mCategoriesView.ShortcutTag})";


            mPluginsView = new FrameView("Scenarios")
            {
                X = 25,
                Y = 1, // for menu
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                CanFocus = true,
                Shortcut = Key.CtrlMask | Key.S
            };
            mPluginsView.Title = $"{mPluginsView.Title} ({mPluginsView.ShortcutTag})";
            mPluginsView.ShortcutAction = () => mPluginsView.SetFocus();

            mListOfCategories = mPlugins.Select(g => g.Key).ToList();

            mCategoriesListView = new ListView(mListOfCategories)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(0),
                Height = Dim.Fill(0),
                AllowsMarking = false,
                CanFocus = true
            };
            mCategoriesListView.SelectedItem = mSelectedCategory;
            mCategoriesListView.OpenSelectedItem += (_) => { mPluginsView.SetFocus(); };
            mCategoriesListView.SelectedItemChanged += CategoryListView_SelectedChanged;
            mCategoriesView.Add(mCategoriesListView);

            mPluginsListView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                AllowsMarking = false,
                CanFocus = true
            };
            mPluginsListView.OpenSelectedItem += PluginsListView_OpenSelectedItem;
            mPluginsView.Add(mPluginsListView);
        }

        public void ShowMenu()
        {
            mTop.RemoveAll();
            mTop.Add(mCategoriesView);
            mTop.Add(mPluginsView);
            Application.Run();
        }

        private void CategoryListView_SelectedChanged(ListViewItemEventArgs e)
        {
            if (e.Value == null)
            {
                return;
            }
            if (mCategoriesListView.SelectedItem != mSelectedCategory)
            {
                mSelectedAlgo = 0;
            }
            mSelectedCategory = mCategoriesListView.SelectedItem;
            var algos = mPlugins[(string)e.Value];
            mPluginsListView.Source = new PluginDataSource(algos);
            mPluginsListView.SelectedItem = mSelectedAlgo;
        }

        private void PluginsListView_OpenSelectedItem(ListViewItemEventArgs e)
        {
            IPlugin plugin = (IPlugin)e.Value;
            plugin.RegisterOnBack(new IPlugin.VoidCallback(ShowMenu));
            plugin.OnEnter(mTop);
        }

    }
}
