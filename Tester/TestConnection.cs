using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using multiplexingThrottler;

namespace Tester
{
    [TestClass]
    public class TestConnection
    {
        // QUICK HACK
        IDictionary<int,bool> _clientConnectionChecked = new Dictionary<int, bool>();
        List<TcpListener> tcps = new List<TcpListener>();

        /** shut down tcp listeners **/
        [TestCleanup()]
        public void Initialize()
        {
            foreach (var tcpListener in tcps)
            {
                tcpListener.Stop();
            }
            tcps.Clear();
            
            Thread.Sleep(3000); //wait for 3 seconds for cleaning up.
        }

        #region Test Connections

        /**
         * Initiate 64 TCP server and construct 64 clients and make sure all of them connected and LOOKED right.
         */
        [TestMethod]
        [TestCategory("connection")]
        public void TestConnection_simple()
        {
            
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            const int baseport = 8000;
            _clientConnectionChecked = new Dictionary<int, bool>();
            for (int i = 0; i < 64; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:"+(port));
                bps.Add(i+1);
                _clientConnectionChecked.Add(port,false);
                tcps.Add(GetTCPLisener(port));
            }
            var b = new Byte[128];
            var mt = new MultiplexThrottler(ips, bps, b);


            mt.ConnectAllDevices();
            Assert.IsTrue(_clientConnectionChecked.Values.All(t => t));
            Assert.AreEqual(64, _clientConnectionChecked.Values.Count);
        }


        /**
         * There is ONE server cannot be connected, see if the client throws application exception
         */
        [TestMethod]
        [ExpectedException(typeof(ApplicationException), "System.ApplicationException: Cannot initiate socket to client 127.0.0.1:8063")]
        [TestCategory("connection")]
        [TestCategory("exception")]
        public void TestConnection_exp()
        {
            var ips = new List<String>();
            var bps = new List<int>();
            const int baseport = 8000;
            _clientConnectionChecked = new Dictionary<int, bool>();
            for (int i = 0; i < 64; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port)); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" ... };
                bps.Add(i + 1);
                _clientConnectionChecked.Add(port, false);
                tcps.Add(GetTCPLisener(port-1));
            }
            var b = new Byte[128];
            var mt = new MultiplexThrottler(ips, bps, b);


            mt.ConnectAllDevices();
            Assert.IsTrue(_clientConnectionChecked.Values.All(t => t));
            Assert.AreEqual(64, _clientConnectionChecked.Values.Count);
        }

        #endregion

        /**
         * Initiate 64 TCP server and construct 64 clients and make sure all of them connected and LOOKED right.
         */
        [TestMethod]
        [TestCategory("send")]
        public void TestSend_simple()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            const int baseport = 8000;
            _clientConnectionChecked = new Dictionary<int, bool>();
            //single server
            for (int i = 0; i < 1; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port));
                bps.Add(i + 1);
                _clientConnectionChecked.Add(port, false);

                var server = new DummyServer(port);
                var oThread = new Thread(new ThreadStart(server.Start));

                liseners.Add(server);

            }
            var b = new Byte[128];

            var mt = new MultiplexThrottler(ips, bps, b);
            Assert.IsTrue(liseners.All(t => t.m.WaitOne(20000)));
            Assert.AreEqual(1, liseners.Count);
            mt.ConnectAllDevices();

            mt.DeliverAllDevices();
        }
        #region test upload

        #endregion


        #region helper methods
        private TcpListener GetTCPLisener(int portNumber)
        {
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, portNumber));
            listener.Start();
            IAsyncResult beginAcceptTcpClient = listener.BeginAcceptTcpClient(OnClientAccepted, listener);
            
            return listener;
        }

        private void OnClientAccepted(IAsyncResult ar)
        {
            var tcpl = ((TcpListener) ar.AsyncState);
            int port = int.Parse(tcpl.LocalEndpoint.ToString().Split(new char[] {':'})[1]);
            var tcpClient = tcpl.EndAcceptTcpClient(ar);

            Console.WriteLine("Connection accepted @"+ port );
            
        }

      

        #endregion

    }
}
