using System;

namespace multiplexingThrottler
{
    public interface IThrottlerPoclicyHandler
    {
        /// <summary>
        /// The completion handler for a data dispatch cycle.
        /// This handler should determine what to do with the device manager and instruct it to send out data immediately or wait
        /// </summary>
        /// <param name="deviceManager">It is a IDeviceManager</param>
        void SendCompleteHandler(IAsyncResult deviceManager);

        /// <summary>
        /// Instruct device manager to send out data  
        /// </summary>
        /// <param name="deviceManager"></param>
        /// <param name="dm"></param>
        void DispatchOneDataCycle(IDeviceManager deviceManager);
    }
}