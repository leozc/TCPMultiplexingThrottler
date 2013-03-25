using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using C5;
namespace multiplexingThrottler
{
   /**
    * This implementation of the policy allows catching up burst
    */
    public class CatchupThrottlerPolicyHandler :SimplePerDataBlockThrottlerPolicyHandler
    {
        public CatchupThrottlerPolicyHandler() :base()
        {
        }

       public override void DispatchOneDataCycle(IDeviceManager dm)
       {
           // Convert the string data to byte data using ASCII encoding.
           ////// get the number of future block
           long byteSent = dm.Metrics.ByteSend;
           long startTSinSecond = dm.Metrics.StartTick / DeviceMetric.TICKPERMS/1000;
           long currentInSecond = dm.Metrics.CurrentTick / DeviceMetric.TICKPERMS / 1000;
           int ratio = 1;
           long diff = currentInSecond - startTSinSecond ;
           if (diff >= 5)// don't do anything for first 5 seconds
           { 
               ratio = (int) (diff *1000 / dm.TimeBlockinMs);
           }

           IAsyncResult r = dm.DeliveryNextBlockOfData(SendCompleteHandler,ratio);
           if (r == null)
               Console.WriteLine(dm.Ipaddr.ToString() + " completed");
       }
   }
}
