using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace multiplexingThrottler
{
    public class MultiplexThrottler
    {
        /// FIELDS
        protected readonly IList<DeviceManager> Ds = new List<DeviceManager>();
        private Byte[] _content;
        private const int SOCKETTIMEOUT = 20000; //20s
        private const int TIMEBLOCKSIZEINMS = 5000; //20s

        private IThrottlerPoclicyHandler policy = new DummyThrottlerPolicyHandler();

        public IList<DeviceManager> DestSpecs
        {
            get { return Ds; }
        }

        ///////////////

        /// <summary>
        /// Construct a throttler
        /// </summary>
        /// <param name="destinations">a list of address in the format of ip:port string</param>
        /// <param name="speedInBps">integer indicate the corresponding speed in destinations</param>
        /// <param name="content">the file represented in byte array</param>
        public MultiplexThrottler(IList<String> destinations, IList<int> speedInBps, Byte[] content)
        {
            if (destinations.Count != speedInBps.Count || speedInBps.Count==0)
                throw new ArgumentException("Destinations and SpeedinBPS must have equal none 0 length.");

            if (content.Length / destinations.Count * destinations.Count != content.Length)
                throw new ArgumentException("The number of bytes must be evenly divided by destination number");

            int blocksize = (content.Length + 1)/ destinations.Count;
            for (int i = 0; i < destinations.Count; i++)
            {
                int startIdx = blocksize * i;
                int endIdx = blocksize * (i + 1) - 1;
                Ds.Add(new DeviceManager(destinations[i], speedInBps[i], content, startIdx, endIdx, TIMEBLOCKSIZEINMS));
            }
            this._content = content;
        }

        #region connection
        /**********************************************************************
         * CONNECTION
         **********************************************************************/

        /// <summary>
        /// Connect to all devices, throw exception if any of these device is failed to connect.
        /// </summary>
        public void ConnectAllDevices()
        {
            foreach (DeviceManager s in Ds)
            {
                CreateClient(s);
            }
            foreach (DeviceManager s in Ds)
            {
                var r = s.InProcess.WaitOne(millisecondsTimeout: SOCKETTIMEOUT);
                if (!r)
                    throw new ApplicationException(String.Format("Cannot initiate socket to client {0}:{1}", s.Ipaddr.ToString(), s.Port));
            }
        }//ConnectAllDevices

        ///Build a socket and start the connection asynchorously, update the atomic token in spec 
        private Socket CreateClient(DeviceManager spec)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            spec.Client = client;
            var result = client.BeginConnect(new IPEndPoint(spec.Ipaddr, spec.Port), ConnectCallback, spec);
            return client;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var cs = (DeviceManager)ar.AsyncState;

                // Complete the connection.
                cs.Client.EndConnect(ar);
                Console.WriteLine("Socket connected to {0}",cs.Client.RemoteEndPoint);
                cs.InProcess.Set(); // signal connection has been made
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        /**********************************************************************
         * END CONNECTION
         **********************************************************************/
        #endregion


        /**********************************************************************
        * DATA DELIVERY
        **********************************************************************/
        /// <summary>
        /// Connect to all devices, throw exception if any of these device is failed to connect.
        /// </summary>
        public void DeliverAllDevices()
        {
            foreach (DeviceManager s in Ds)
            {
                // start transmit
                policy.DispatchOneDataCycle(s);
            }
        }
     
        
        /**********************************************************************
        * END OF DATA DELIVERY
        **********************************************************************/
    }

}
