using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OAFramework.WebSocket.utils
{
    public class Util
    {
        public static IPAddress GetIPV4()
        {
            var hostName = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);
            foreach (var x in hostEntry.AddressList) if (x.AddressFamily.Equals(AddressFamily.InterNetwork)) return x;
            return hostEntry.AddressList[0];
        }
    }
}
