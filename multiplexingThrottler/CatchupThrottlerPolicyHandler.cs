using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using C5;
namespace multiplexingThrottler
{
   /**
    * This implementation of the policy allows catching up with limited burst (token bucket)
    */
    public class CatchupThrottlerPolicyHandler :SimplePerDataBlockThrottlerPolicyHandler
    {
        const int BURSTMAX=5;
        public CatchupThrottlerPolicyHandler() :base()
        {
        }

       public override void DispatchOneDataCycle(IDeviceManager dm)
       {
           int numberOfBlock = 1;

           if (dm.ExpectedByteSent >= dm.ContentSizeForOperate)
               numberOfBlock = 1;
//calculate the burst blocks.
           else if (dm.ExpectedByteSent - dm.Metrics.ByteSent >= dm.SpeedInBytePerTimeBlock)
           {
               numberOfBlock = (int)(dm.ExpectedByteSent - dm.Metrics.ByteSent) / dm.SpeedInBytePerTimeBlock;
           }

           if (numberOfBlock > BURSTMAX)
               numberOfBlock = BURSTMAX; // don't do more than BURSTMAX blocks
           
           IAsyncResult r = dm.DeliveryNextBlockOfData(SendCompleteHandler,numberOfBlock);
           if (r == null)
               Console.WriteLine(dm.Ipaddr.ToString() + " completed");
       }
   }
}
