using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace multiplexingThrottler
{
     public enum  DeviceState
     {
         Init = 1,
         Duringconnection = 5,
         AfterConnection =10,
         Duringsend = 15,
         Completesend = 20,
         Completed = 25,
         Error =255,
     }
    public interface IDeviceManager :IComparable
    {
       
        int SpeedInBitPerSecond { get; }
        IPAddress Ipaddr { get; }
        int Port { get; }
        /**
         * A mutex, and it would receive a signal when connection is ready
         */
        ManualResetEvent ConnectionReadySignal { get; }
        /**
         * A mutex, and it would receive a signal when all data deliverred.
         */
        ManualResetEvent SendCompleteSignal { get; }
 
        /// <summary>
        /// Device Metric object, this important data structure can be used to do 
        /// throttling control
        /// </summary>
        DeviceMetric Metrics { get; }

        /// <summary>
        /// Bandwidth Control Granulity level minimum is 1000ms. 
        /// </summary>
        int TimeBlockinMs { get; }
        long ExpectedByteSent {get;}
        int SpeedInBytePerTimeBlock { get; set; }
        int ContentSizeForOperate { get; }
        /**
         * Return current state of e device.
         */
        DeviceState GetDeviceState();

        #region Connection Creation
        /**
         * Create Client and connect to the remove device.
         */
        Socket CreateClient();
        IAsyncResult AsyncBeginConnect(AsyncCallback connectCallback);
        /**
         * Call this method when connection event is completed
         */
        void CompleteConnectionReadySignal(IAsyncResult r);
        #endregion

        #region DATA DELIVERY
        /// <summary>
        /// Delivery next data blocks to the socket and sendCallback is called when it is done
        /// </summary>
        /// <param name="sendCallback">callback</param>
        /// <param name="numberOfBlock">number of TIMEBLOCKINMS worth data. For example if TimeBlockinMS is 5000ms, and speed is classified as 1024bps, and the 
        /// rate control will be set to 5120 bits per 5 seconds. and if numberOfBlock is 10, that means the function should dispatch 5120*10 worth of data to the socket pipe.
        /// </param>
        /// <returns></returns>
        IAsyncResult DeliveryNextBlockOfData(AsyncCallback sendCallback, int numberOfBlock = 1);
        int CompleteOneDataCycle(IAsyncResult r);
        #endregion
    }
}