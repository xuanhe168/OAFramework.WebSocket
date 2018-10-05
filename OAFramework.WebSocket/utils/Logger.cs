using System;
using System.IO;

namespace OAFramework.WebSocket
{
    internal class Logger
    {
        public bool enabled = true;
        public void d(string text)
        {
            if (enabled) Console.WriteLine(text);
        }
        public void Write(string text)
        {
            var curDir = Environment.CurrentDirectory;
            var fn = Path.Combine(curDir, "log", DateTime.Now.ToString() + ".log");
            File.WriteAllText(fn, text);
        }
    }
}
