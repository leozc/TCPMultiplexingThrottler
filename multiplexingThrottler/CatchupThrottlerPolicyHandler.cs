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
           long startTSinMS = dm.Metrics.StartTick / DeviceMetric.TICKPERMS;
           long lastTSinMS = dm.Metrics.LastTick / DeviceMetric.TICKPERMS;
           long currentInMS = dm.Metrics.CurrentTick / DeviceMetric.TICKPERMS ;
           int numberOfBlock = 1;

           if (dm.ExpectedByteSent >= dm.ContentSizeForOperate)
               numberOfBlock = 1;
           else if (dm.ExpectedByteSent - dm.Metrics.ByteSend >= dm.SpeedInBytePerTimeBlock)
           {
               numberOfBlock = (int)(dm.ExpectedByteSent - dm.Metrics.ByteSend) / dm.SpeedInBytePerTimeBlock;
           }

           if (numberOfBlock > 5)
               numberOfBlock = 5; // don't do more than 10 block
           


           IAsyncResult r = dm.DeliveryNextBlockOfData(SendCompleteHandler,numberOfBlock);
           if (r == null)
               Console.WriteLine(dm.Ipaddr.ToString() + " completed");
       }

   }
}
