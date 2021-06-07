using Serilog;
using System;
using System.IO;

namespace PluginInterface.Logger
{
    public class Logger
    {
        private static Logger sInstance;
        public static Logger Instance
        {
            get
            {
                if (sInstance == null)
                {
                    sInstance = new Logger();
                }
                return sInstance;
            }
        }

        private Logger()
        {
            string logFileName = DateTime.UtcNow.ToShortTimeString() + "-" + DateTime.UtcNow.ToShortDateString() + ".txt";
            logFileName = logFileName.Replace(":", "-").Replace(" ", "-").Replace("/", "-");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine("logs", logFileName)).CreateLogger();
        }

        public void Information(string log)
        {
            Log.Information(log);
        }

        public void Error(string log)
        {
            Log.Error(log);
        }
        public void Warning(string log)
        {
            Log.Warning(log);
        }
    }
}
