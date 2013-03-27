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
        const int Mintimeblock = 1000;
        public int SpeedInBitPerSecond { get; private set; }
        private readonly IPAddress _ipaddr;
        private readonly int _port;
        private readonly Byte[] _content;
        private readonly int _timeBlockinMs;

        /**
         * Rate control resolution
         */
        public int SpeedInBytePerTimeBlock { get; set; }

        private readonly ManualResetEvent _connectionReadySignal;
        private readonly ManualResetEvent _sendCompleteSignal;
        private readonly DeviceMetric _metrics;

        #region idx for content
        private int _offsetIdx;
        private readonly int _endIdx;
        private readonly int _startIdx;
        public int ContentSizeForOperate { get { return _endIdx - _startIdx; } }
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

        public ManualResetEvent ConnectionReadySignal
        {
            get { return _connectionReadySignal; }
        }

        public ManualResetEvent SendCompleteSignal { get { return _sendCompleteSignal; } }

        public Socket Client { get; set; }

        public int EndIdx
        {
            get { return _endIdx; }
        }

        public int StartIdx
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


        public long ExpectedByteSent
        {
            get { 

                long result = (Metrics.CurrentTick - Metrics.StartTick) / DeviceMetric.TICKPERMS / 1000 * SpeedInBitPerSecond / 8;
                return result >= EndIdx - StartIdx ? EndIdx - StartIdx : result;
                }
        }

        #endregion

        /// <summary>
        /// Construct a devicemanager that keeps the state of remove device
        /// </summary>
        /// <param name="dest">the address of device manager in the form IP:PORT</param>
        /// <param name="speedInBps">the numerical representation for the speed in term of bit per second</param>
        /// <param name="content">the contnet byte array</param>
        /// <param name="startIdx">the start of the index slice of the contnent inclusive</param>
        /// <param name="endIdx">the end of the index slice of the contnent exclusive</param>
        /// <param name="timeBlockinMs">the granularity of the rate control measure block. for example 2000 means the rate control block is 2*speedInBps bits per 2 second </param>
        public DeviceManager(string dest, int speedInBps, Byte[] content, int startIdx, int endIdx, int timeBlockinMs)
        {
            if (timeBlockinMs < Mintimeblock)
                throw new ArgumentException("TimeBlock Resolution must be larger than 1000ms, ideally 5000-10000 ms");

            String[] a = dest.Split(new char[] { ':' });
            _ipaddr = IPAddress.Parse(a[0]); // throw exception when it is illegal port number anyway
            _port = int.Parse(a[1]);
            SpeedInBitPerSecond = speedInBps;
            _connectionReadySignal = new ManualResetEvent(false);
            _sendCompleteSignal = new ManualResetEvent(false);
            _content = content;
            _startIdx = startIdx;
            _endIdx = endIdx;
            _timeBlockinMs = timeBlockinMs;
            _offsetIdx = startIdx;
            _metrics = new DeviceMetric();
            _metrics.TotalByte = endIdx - startIdx;
            // e.g. if bps = 8 and timeblockinMS is 5000(5 seconds) it is equal to 40 byte per 5 seconds //
            SpeedInBytePerTimeBlock = (int)(SpeedInBitPerSecond / 8.0* (timeBlockinMs / 1000.0));
           
        }

        DeviceState _deviceState = DeviceState.Init;
        public DeviceState GetDeviceState()
        {
            return _deviceState;
        }

        public int CompareTo(object obj)
        {
            var m = obj as IDeviceManager;
            if (m == null)
                throw new ArgumentException("Expect IDeviceManage Object in CompareTo function");
            return (int)(m.Metrics.LastTick - this.Metrics.LastTick);
        }

        #region
        public Socket CreateClient()
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Client = client;
            return client;
        }
        public IAsyncResult AsyncBeginConnect(AsyncCallback connectCallback)
        {
            this._deviceState = DeviceState.Duringconnection;
            return Client.BeginConnect(new IPEndPoint(this.Ipaddr, this.Port), connectCallback, this);
        }
        public void CompleteConnectionReadySignal(IAsyncResult r)
        {
            ConnectionReadySignal.Set();
            // Complete the connection.
            Client.EndConnect(r);
            this._deviceState = DeviceState.AfterConnection;
        }
        #endregion

        #region Data delivery
        public IAsyncResult DeliveryNextBlockOfData(AsyncCallback sendCallback, int numberOfBlock = 1)
        {
            // Begin sending the data to the remote device.
            var index = OffsetIdx; // must called before GetNextDataTransferBlockSize

            var dataBlockSizeInByte = 0;
            for (var i = 0; i < numberOfBlock; i++)
                dataBlockSizeInByte += GetNextDataTransferBlockSize();

            if (_metrics.StartTick == 0)
                _metrics.StartTick = DateTime.Now.Ticks;

            _metrics.LastTick = DateTime.Now.Ticks;
            if (dataBlockSizeInByte != 0)
            {
                this._metrics.ByteSent = this._metrics.ByteSent + dataBlockSizeInByte; // pre-add the write count;
                this._deviceState = DeviceState.Duringsend;
                try
                {
                    return Client.BeginSend(_content, index, dataBlockSizeInByte, SocketFlags.None,
                                        sendCallback, this);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(this + "Error @ close " + e.Message);
                    _deviceState = DeviceState.Error;
                    return null;
                }
            }
            else
            {
                try
                {
                    Client.Close(3000); //job done!
                    _deviceState = DeviceState.Completesend;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(this + "Error @ close " + e.Message);
                    _deviceState = DeviceState.Error;
                }
                finally
                {
                    _sendCompleteSignal.Set();
                  
                }
                return null;
            }
        }
        public int CompleteOneDataCycle(IAsyncResult r)
        {
            int byteTransferred = 0;
            try
            {
                this._deviceState = DeviceState.Duringsend;
                byteTransferred = Client.EndSend(r);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                _deviceState = DeviceState.Error;
            }
            return byteTransferred;
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
        #endregion
        public override string ToString()
        {
            return String.Format("DeviceManaer:IP={0}:PORT={1}:DeviceState={2}:SpeedInBytePerTimeBlock={3}:ExpectedByteSent:{4}:ByteSent:{5}",
                this.Ipaddr, this.Port, this._deviceState, this.SpeedInBytePerTimeBlock, this.ExpectedByteSent,this._metrics.ByteSent);
        }
    }
}
