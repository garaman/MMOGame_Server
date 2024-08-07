﻿using DummyClient.Session;
using ServerCore;
using System.Net;

namespace DummyClient
{

    internal class Program
    {
        static int DummyClientCount { get; } = 20;
        static void Main(string[] args)
        {
            Thread.Sleep(3000);
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            Connector connector = new Connector();

            connector.Connect(endPoint,
                () => { return SessionManager.Instance.Generate(); },
                Program.DummyClientCount);

            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
