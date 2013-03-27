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
    public class MultiplexThrottler<T> where T :IThrottlerPoclicyHandler, new()
    {
        /// FIELDS
        protected readonly IList<IDeviceManager> _deviceManagers = new List<IDeviceManager>();
        private Byte[] _content;
        private const int SOCKETTIMEOUT = 20000; //20s
        private const int TIMEBLOCKSIZEINMS = 3000; //5s

        private IThrottlerPoclicyHandler _policy ;

        public IList<IDeviceManager> DeviceManagers
        {
            get { return _deviceManagers; }
        }
        
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

            int blocksize = (content.Length )/ destinations.Count;
            for (int i = 0; i < destinations.Count; i++)
            {
                int startIdx = blocksize * i;
                int endIdx = blocksize * (i + 1) ;//none-inclusive
                _deviceManagers.Add(new DeviceManager(destinations[i], speedInBps[i], content, startIdx, endIdx, TIMEBLOCKSIZEINMS));
            }
            this._content = content;
            _policy = new T();
        }

        public MultiplexThrottler<T> SetThrottlePolicyHandler(T handler)
        {
            _policy = handler;
            return this;
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
            foreach (var s in _deviceManagers)
            {
                s.CreateClient();
                s.AsyncBeginConnect(ConnectCallback);
            }
            foreach (var s in _deviceManagers) // make sure all clients are connected
            {
                var r = s.ConnectionReadySignal.WaitOne(millisecondsTimeout: SOCKETTIMEOUT);
                if (!r)
                    throw new ApplicationException(String.Format("Cannot initiate socket to client {0}:{1}", s.Ipaddr.ToString(), s.Port));
            }
        }//ConnectAllDevices

        ///Build a socket and start the connection asynchorously, update the atomic token in spec 
        

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var cs = (DeviceManager)ar.AsyncState;

                Console.WriteLine("Socket connected to {0}",cs.Client.RemoteEndPoint);
                cs.CompleteConnectionReadySignal(ar); // signal connection has been made
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


        #region kick off data deliver to all devices
        /**********************************************************************
        * DATA DELIVERY
        **********************************************************************/
        /// <summary>
        /// Connect to all devices, throw exception if any of these device is failed to connect.
        /// </summary>
        public void DeliverAllDevices()
        {
            foreach (DeviceManager s in _deviceManagers)
            {
                // start transmit
                _policy.DispatchOneDataCycle(s);
            }
        }
        /**********************************************************************
        * END OF DATA DELIVERY
        **********************************************************************/
        #endregion
    }

}
