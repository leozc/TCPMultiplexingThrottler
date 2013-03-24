using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace multiplexingThrottler
{
    public interface IDeviceManager
    {
        int SpeedInBitPerSecond { get; }
        IPAddress Ipaddr { get; }
        ManualResetEvent InProcess { get; }
        DeviceMetric Metrics { get; }
        int TimeBlockinMs { get; }
        IAsyncResult DeliveryNextBlockOfData(AsyncCallback sendCallback);
    }
}