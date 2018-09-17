using System;

namespace OAFramework.WebSocket
{
    internal class Logger
    {
        public bool enabled = true;
        public void d(string text)
        {
#if DEBUG
            if (enabled) Console.WriteLine(text);
#endif
        }
    }
}
