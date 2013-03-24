using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace multiplexingThrottler
{

    /**
     * Represnet states of the TCP device and their specification.
     */
    public class DeviceManager : IDeviceManager
    {
        const int MINTIMEBLOCK = 1000;
        public int SpeedInBitPerSecond { get; private set; }    
        private readonly IPAddress _ipaddr;
        private readonly int _port;
        private readonly Byte[] _content;
        private readonly int _timeBlockinMs;
        
        
        /**
         * Rate control resolution
         */
        private int SpeedInBytePerTimeBlock { get; set; }
       
        private readonly ManualResetEvent _inProcess;
        private readonly DeviceMetric _metrics;

        #region idx for content
        private int _offsetIdx;
        private readonly int _endIdx;
        private readonly int _startIdx;
        #endregion



        #region GETTER&SETTERS
        public IPAddress Ipaddr
        {
            get { return _ipaddr; }
        }

        public int Port
        {
            get { return _port; }
        }

        public ManualResetEvent InProcess
        {
            get { return _inProcess; }
        }

        public Socket Client { get; set; }

        private int EndIdx
        {
            get { return _endIdx; }
        }

        private int StartIdx
        {
            get { return _startIdx; }
        }

        private int OffsetIdx
        {
            get { return _offsetIdx; }
        }

        public DeviceMetric Metrics
        {
            get { return _metrics; }
        }

        public int TimeBlockinMs
        {
            get { return _timeBlockinMs; }
        }
        #endregion




        public DeviceManager(string dest, int speedInBps, Byte[] content, int startIdx, int endIdx, int timeBlockinMs)
        {
            if (timeBlockinMs < MINTIMEBLOCK)
                throw new ArgumentException("TimeBlock Resolution must be larger than 1000ms, ideally 5000-10000 ms");

            String[] a = dest.Split(new char[] { ':' });
            _ipaddr = IPAddress.Parse(a[0]); // throw exception when it is illegal port number anyway
            _port = int.Parse(a[1]);
            SpeedInBitPerSecond = speedInBps;
            _inProcess = new ManualResetEvent(false);
            _content = content;
            _startIdx = startIdx;
            _endIdx = endIdx;
            _timeBlockinMs = timeBlockinMs;
            _offsetIdx = startIdx;
            _metrics = new DeviceMetric();
            // e.g. if bps = 8 and timeblockinMS is 5000(5 seconds) it is equal to 40 byte per 5 seconds //
            SpeedInBytePerTimeBlock = (int) (SpeedInBitPerSecond/1000.0/8.0*timeBlockinMs);
        }


        public IAsyncResult DeliveryNextBlockOfData(AsyncCallback sendCallback)
        {
            // Begin sending the data to the remote device.
            var index = OffsetIdx; // must called before GetNextDataTransferBlockSize
            var datablockSize = GetNextDataTransferBlockSize();
            
            _metrics.LastTick = DateTime.Now.Ticks;
            if (datablockSize != 0)
            {
                this._metrics.ByteSend = this._metrics.ByteSend + datablockSize; // pre add.
                return Client.BeginSend(_content, index, datablockSize, SocketFlags.None,
                                        sendCallback, this);
            }
            else
            {
                Client.Close();
                return null;
            }
        }

        /// <summary>
        /// Get next data transfer block size, and move offsetIndex correspondingly
        /// </summary>
        /// <returns>
        /// Size of the block in number of bytes and this number is between 0 and SpeedInBytePerTimeBlock inclusive. 
        /// 0 indicates no more data needs to be deliverred. 
        /// </returns>
        private int GetNextDataTransferBlockSize()
        {
            if (OffsetIdx >= EndIdx)
            {
                return 0; // done
            }
            var diff = EndIdx - OffsetIdx;
            _offsetIdx += SpeedInBytePerTimeBlock;
            return _offsetIdx >= EndIdx ? diff : SpeedInBytePerTimeBlock;
        }
    }

    public class DeviceMetric
    {
        private long _startTick;
        private long _lastTick;
        private long _byteSend;
        private long _totalByte;
        
        /***** properties *****/
        public long CurrentTS
        {
            get { return DateTime.Now.Ticks; }
        }

        public long StartTick
        {
            get { return _startTick; }
            set { _startTick = value; }
        }

        public long ByteSend
        {
            get { return _byteSend; }
            set { _byteSend = value; }
        }

        public long LastTick
        {
            get { return _lastTick; }
            set { _lastTick = value; }
        }

        public long TotalByte
        {
            get { return _totalByte; }
            set { _totalByte = value; }
        }
    }
}