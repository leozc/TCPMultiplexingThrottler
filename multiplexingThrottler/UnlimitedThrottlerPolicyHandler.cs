using System;
using System.Threading;
using System.Net.Sockets;
namespace multiplexingThrottler
{
   /**
    * Dummy Sleep timerbased throttler, does nothing good but testing
    */
   public class UnlimitedThrottlerPolicyHandler : IThrottlerPoclicyHandler
   {
       public void DispatchOneDataCycle(IDeviceManager dm)
       {
           // Convert the string data to byte data using ASCII encoding.
           IAsyncResult r = dm.DeliveryNextBlockOfData(SendCompleteHandler);
           if (r == null)
               Console.WriteLine(dm.Ipaddr.ToString() + " completed");
       }
       public void SendCompleteHandler(IAsyncResult deviceManager)
       {
           try
           {
               var dm = deviceManager.AsyncState as DeviceManager;
               if (dm == null)
                   throw new ArgumentException("Hey what is wrong here? The DispatchOneDataCycle put in wrong arg??? Found: "+deviceManager.GetType());
               var byteTransferred = dm.CompleteOneDataCycle(deviceManager);

              // Thread.Sleep(2);
               DispatchOneDataCycle(dm);
           }
           catch (Exception e)
           {
               Console.WriteLine(e.ToString());
           }
       }
   }
}
