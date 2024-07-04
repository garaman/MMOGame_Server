using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static System.Collections.Specialized.BitVector32;
using ServerCore;
using Server.Session;

namespace Server
{
    internal class Program
    {
        static Listener _listener = new Listener();
        public static GameRoom Room = new GameRoom();
         
        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);


            _listener.Init(endPoint, () => { return SessionManager.instance.Generate(); });
            Console.WriteLine("Listening....");

            while (true)
            {
                Room.Push(() => Room.Flush());
                Thread.Sleep(250);
            }


        }
    }
}
