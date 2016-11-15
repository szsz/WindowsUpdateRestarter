using System;
using System.IO;

namespace WindowsUpdateRestarter
{
    public static class Logger
    {
        private static string ProvidePath(string filename)
        {
            var di = new DirectoryInfo(Path.GetDirectoryName(filename));
            di.Create();
            return filename;
        }

        private static Lazy<string> LogFn = new Lazy<string>(
            () => ProvidePath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Tresorit\WindowsUpdateRestarter_log.txt"));

        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogFn.Value, "[" + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff") + "] " + msg + "\r\n");
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to log `{msg}`: {e}");
            }
        }
    }
}