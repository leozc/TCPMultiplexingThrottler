using System;
using System.Threading;

namespace multiplexingThrottler
{
   public class DummyThrottlerPolicyHandler : IThrottlerPoclicyHandler
   {

       public void DispatchOneDataCycle(DeviceManager dm)
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
               var dm = deviceManager as DeviceManager;
               if (dm == null)
                   throw new ArgumentException("Hey what is wrong here? The DispatchOneDataCycle put in wrong arg??? Found: "+deviceManager.GetType());
               dm.Client.EndSend(deviceManager);
               Thread.Sleep(500);
               DispatchOneDataCycle(dm);
           }
           catch (Exception e)
           {
               Console.WriteLine(e.ToString());
           }
       }

   }
}
