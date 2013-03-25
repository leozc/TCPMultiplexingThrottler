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
    /***
     * TODO : Repeat Code in test cases, need clean up.
     */
    [TestClass]
    public class TestConnection
    {
        // QUICK HACK
        IDictionary<int,bool> _clientConnectionChecked = new Dictionary<int, bool>();
        List<TcpListener> tcps = new List<TcpListener>();
        private Random rnd = new Random();
        /** shut down tcp listeners **/
        [TestCleanup()]
        public void Initialize()
        {
            foreach (var tcpListener in tcps)
            {
                try
                {
                    tcpListener.Stop();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Ignore Initialzer @ " + e.Message);
                }
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
            _clientConnectionChecked.Clear();
            for (int i = 0; i < 64; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:"+(port));
                bps.Add(i+1);
                _clientConnectionChecked.Add(port,false);
                tcps.Add(GetTcpLisener(port));
            }
            var b = new Byte[128];
            var mt = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, b);


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
            _clientConnectionChecked.Clear();
            for (int i = 0; i < 64; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port)); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" ... };
                bps.Add(i + 1);
                _clientConnectionChecked.Add(port, false);
                tcps.Add(GetTcpLisener(port-1));
            }
            var b = new Byte[128];
            var mt = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, b);


            mt.ConnectAllDevices();
            Assert.IsTrue(_clientConnectionChecked.Values.All(t => t));
            Assert.AreEqual(64, _clientConnectionChecked.Values.Count);
        }

        #endregion


        #region test Send
        /**
         * Simple test for 1 Device with small buffer
         */
        [TestMethod]
        [TestCategory("send")]
        public void TestSend_simple()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            const int baseport = 8000;
            var size = 128;
            var deviceCount = 1;
            _clientConnectionChecked = new Dictionary<int, bool>();
            Console.WriteLine("START");
            //single server
            for (int i = 0; i < deviceCount; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port));
                bps.Add((i + 1) * 1000);
                //_clientConnectionChecked.Add(port, false);

                var server = new DummyServer(port);
                var oThread = new Thread(new ThreadStart(server.Start));
                oThread.Start();
                liseners.Add(server);

            }
            var content = genArray(size);

            var mt = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, content);

            mt.ConnectAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketAccepted.WaitOne(10000)));
            Assert.AreEqual(deviceCount, liseners.Count);
            mt.DeliverAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketCompleted.WaitOne(10000)));
            DeepVerify(liseners, size, deviceCount, content);
        }




        /**
      * Simple test for 1 Device with larger buffer
      */
        [TestMethod]
        [TestCategory("send")]
        public void TestSend_simple_largeBuff()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            const int baseport = 8000;
            var size = 128*20;
            _clientConnectionChecked = new Dictionary<int, bool>();
            
            Console.WriteLine("START");
            const int deviceCount = 1;
            //single server
            for (int i = 0; i < deviceCount; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port));
                bps.Add((i + 1) * 1000);
                //_clientConnectionChecked.Add(port, false);

                var server = new DummyServer(port);
                var oThread = new Thread(new ThreadStart(server.Start));
                oThread.Start();
                liseners.Add(server);

            }
            var content = genArray(size);

            var mt = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, content);

            mt.ConnectAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketAccepted.WaitOne(10000)));
            Assert.AreEqual(deviceCount, liseners.Count);
            mt.DeliverAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketCompleted.WaitOne(30000)),"Fail to finish reading?");
            Assert.IsTrue(liseners.All(t => t.GetBytes().Length == size), "Byte Array length nor euqal? expect=" + size + " actual="+liseners[0].GetBytes().Length);

            VerifyMetrics(mt, size, deviceCount);

            DeepVerify(liseners, size, deviceCount, content);
        }


        /**
         * Simple test for 3 Device with larger buffer
         */
        [TestMethod]
        [TestCategory("send")]
        public void TestSend_multiple_largeBuff()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            var deviceCount = 3;
            const int baseport = 8000;
            var size = 30000;
            _clientConnectionChecked = new Dictionary<int, bool>();
            Console.WriteLine("START");
            //single server
            for (int i = 0; i < deviceCount; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port));
                bps.Add((i + 1) * 1000);
                //_clientConnectionChecked.Add(port, false);

                var server = new DummyServer(port);
                var oThread = new Thread(new ThreadStart(server.Start));
                oThread.Start();
                liseners.Add(server);

            }
            var content = genArray(size);

            var mt = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, content);

            mt.ConnectAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketAccepted.WaitOne(10000)));
            Assert.AreEqual(deviceCount, liseners.Count);
            mt.DeliverAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketCompleted.WaitOne(30000)), "Fail to finish reading?");
            DeepVerify(liseners, size, deviceCount, content);
            VerifyMetrics(mt, size, deviceCount);
        }

     

        #endregion


        #region Policy
        [TestMethod]
        [TestCategory("policy")]
        public void TestSend_simplePerDataBlock_correctness()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            var deviceCount = 1;
            const int baseport = 8000;
            var size = 30000;
            _clientConnectionChecked = new Dictionary<int, bool>();
            Console.WriteLine("START");
            //single server
            for (int i = 0; i < deviceCount; i++)
            {
                int port = baseport + i;
                ips.Add("127.0.0.1:" + (port));
                bps.Add((i + 1) * 1000);
                //_clientConnectionChecked.Add(port, false);

                var server = new DummyServer(port);
                var oThread = new Thread(new ThreadStart(server.Start));
                oThread.Start();
                liseners.Add(server);

            }
            var content = genArray(size);

            var mt = new MultiplexThrottler<SimplePerDataBlockThrottlerPolicyHandler>(ips, bps, content);

            mt.ConnectAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketAccepted.WaitOne(10000)));
            Assert.AreEqual(deviceCount, liseners.Count);
            mt.DeliverAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketCompleted.WaitOne(300000)), "Fail to finish reading?");
            DeepVerify(liseners, size, deviceCount, content);
            VerifyMetrics(mt, size, deviceCount);
        }


        [TestMethod]
        [TestCategory("policy")]
        [TestCategory("integration")]
        [TestCategory("manual")]
        public void TestSend_simplePerDataBlock_manual1()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            var deviceCount = 4;
            const int baseport = 8000;
            var size = 1024*1024*64;
            _clientConnectionChecked = new Dictionary<int, bool>();
            Console.WriteLine("START");
            //single server
            for (int i = 0; i < deviceCount; i++)
            {
                int port = baseport + i;
                ips.Add("192.168.1.7:" + (port));
                //ips.Add("127.0.0.1:" + (port));
                bps.Add((i + 1) * 1024*1024);
                //_clientConnectionChecked.Add(port, false);

            }
            var content = genArray(size);

            var mt = new MultiplexThrottler<SimplePerDataBlockThrottlerPolicyHandler>(ips, bps, content);

            mt.ConnectAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketAccepted.WaitOne(10000)));
           // Assert.AreEqual(deviceCount, liseners.Count);
            mt.DeliverAllDevices();
            Assert.IsTrue(mt.DeviceManagers.All(t => t.SendCompleteSignal.WaitOne(300000)), "Fail to finish reading?");
            //DeepVerify(liseners, size, deviceCount, content);
            VerifyMetrics(mt, size, deviceCount);
        }
        [TestMethod]
        [TestCategory("policy")]
        [TestCategory("integration")]
        [TestCategory("manual")]
        public void TestSend_simplePerDataBlock_manual2()
        {
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            var liseners = new List<DummyServer>();
            var deviceCount = 64;
            const int baseport = 8000;
            var size = 1024 * 1024 * 64;
            _clientConnectionChecked = new Dictionary<int, bool>();
            Console.WriteLine("START");
            //single server
            for (int i = 0; i < deviceCount; i++)
            {
                int port = baseport + i;
                ips.Add("192.168.1.7:" + (port));
                //ips.Add("127.0.0.1:" + (port));
                bps.Add((i + 1) * 1024 * 500);
                //_clientConnectionChecked.Add(port, false);

            }
            var content = genArray(size);

            var mt = new MultiplexThrottler<SimplePerDataBlockThrottlerPolicyHandler>(ips, bps, content);

            mt.ConnectAllDevices();
            Assert.IsTrue(liseners.All(t => t.socketAccepted.WaitOne(10000)));
            // Assert.AreEqual(deviceCount, liseners.Count);
            mt.DeliverAllDevices();
            Assert.IsTrue(mt.DeviceManagers.All(t => t.SendCompleteSignal.WaitOne(300000)), "Fail to finish reading?");
            //DeepVerify(liseners, size, deviceCount, content);
            VerifyMetrics(mt, size, deviceCount);
        }

        #endregion 

        #region helper methods
        private TcpListener GetTcpLisener(int portNumber)
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Any, portNumber));
            listener.Start();
            IAsyncResult beginAcceptTcpClient = listener.BeginAcceptTcpClient(OnClientAccepted, listener);
            
            return listener;
        }

        private void OnClientAccepted(IAsyncResult ar)
        {
            var tcpl = ((TcpListener)ar.AsyncState);
            var port = int.Parse(tcpl.LocalEndpoint.ToString().Split(new char[] { ':' })[1]);
            try
            {
                var tcpClient = tcpl.EndAcceptTcpClient(ar);

                System.Diagnostics.Debug.WriteLine("Connection accepted @" + port);
                _clientConnectionChecked[port] = true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception in server " + port + " "+e.Message);
            }
        }

        /**
         * easier to spot start and end of the each buffer :)
         */
        private static void SetDeedBeef(byte[] content, int size)
        {
            content[0] = Convert.ToByte('D');
            content[1] = Convert.ToByte('E');
            content[2] = Convert.ToByte('A');
            content[3] = Convert.ToByte('D');

            content[size - 4] = Convert.ToByte('B');
            content[size - 3] = Convert.ToByte('E');
            content[size - 2] = Convert.ToByte('E');
            content[size - 1] = Convert.ToByte('F');
        }


        private void VerifyMetrics<T>(MultiplexThrottler<T> mt, int size, int deviceCount) where T : IThrottlerPoclicyHandler, new()
        {
            Assert.IsTrue(mt.DeviceManagers.All(t => t.Metrics.TotalByte == size / deviceCount), "TotalByte counter incorrect");
            Assert.IsTrue(mt.DeviceManagers.All(t => t.Metrics.TotalByte == t.Metrics.ByteSend), "ByteSend counter incorrect");
            foreach (IDeviceManager d in  mt.DeviceManagers){
                System.Diagnostics.Debug.WriteLine(d.Ipaddr+":"+d.Metrics.CurrentBitPerSecond);
            }

        }

        private void DeepVerify(List<DummyServer> liseners, int size, int deviceCount, byte[] content)
        {
            for (int i = 0; i < liseners.Count; i++)
            {
                var clientReceived = liseners[i].GetBytes();
                Assert.AreEqual(size / deviceCount, clientReceived.Count(),
                                "Byte Array length nor euqal? expect=" + size + " actual=" +
                                liseners[i].GetBytes().Length + " nth=" + i);

                var blocksize = size / deviceCount;
                for (var p = i * blocksize; p < i * blocksize + size / deviceCount; p++)
                {
                    //System.Diagnostics.Debug.WriteLine(" i=" + i + " p=" + p + " blocksize=" + blocksize + " block=" + size / deviceCount);
                    Assert.AreEqual(content[p], clientReceived[p - i * blocksize]);
                }
            }
        }

        private byte[] genArray(int size)
        {
            var content = new Byte[size];
            for (int i = 0; i < content.Length; i++)
            {
                content[i]=(byte)(i%255);
            }
            SetDeedBeef(content, size);
            return content;
        }
        #endregion

    }
}
