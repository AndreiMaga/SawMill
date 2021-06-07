using NStack;
using PluginInterface.Plugin;
using System.Collections;
using System.Collections.Generic;
using Terminal.Gui;

namespace SawMill.App.Menu
{
    class PluginDataSource : IListDataSource
    {
        private int mNameColumnWidth = 30;
        private List<IPlugin> mPlugins;
        private BitArray mMarks;
        private int mCount, mLen;

        public List<IPlugin> Plugins
        {
            get => mPlugins;
            set
            {
                if (value != null)
                {
                    mCount = value.Count;
                    mMarks = new BitArray(mCount);
                    mPlugins = value;
                    mLen = GetMaxLengthItem();
                }
            }
        }
        public PluginDataSource(List<IPlugin> plugins) => mPlugins = plugins;

        private int GetMaxLengthItem()
        {
            if (mPlugins?.Count == 0)
            {
                return 0;
            }

            int maxLength = 0;
            foreach (var plugin in mPlugins)
            {
                var s = string.Format(string.Format("{{0,{0}}}", -mNameColumnWidth), plugin.GetName());
                var sc = $"{s} {plugin.GetDescription()}";
                int len = sc.Length;
                if (len > maxLength)
                {
                    maxLength = len;
                }
            }
            return maxLength;
        }

        public int Count => Plugins != null ? Plugins.Count : 0;

        public int Length => mLen;

        public bool IsMarked(int item)
        {
            if (item >= 0 && item < mCount)
                return mMarks[item];
            return false;
        }

        public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
        {
            container.Move(col, line);
            var plugin = Plugins[item];
            var s = string.Format(string.Format("{{0,{0}}}", -mNameColumnWidth), plugin.GetName());
            RenderUstr(driver, $"{s} {plugin.GetDescription()}", col, line, width, start);

        }
        private void RenderUstr(ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
        {
            int used = 0;
            int index = start;
            while (index < ustr.Length)
            {
                (var rune, var size) = Utf8.DecodeRune(ustr, index, index - ustr.Length);
                var count = System.Rune.ColumnWidth(rune);
                if (used + count >= width) break;
                driver.AddRune(rune);
                used += count;
                index += size;
            }

            while (used < width)
            {
                driver.AddRune(' ');
                used++;
            }
        }

        public void SetMark(int item, bool value)
        {
            if (item >= 0 && item < mCount)
                mMarks[item] = value;
        }

        public IList ToList()
        {
            return Plugins;
        }
    }
}
