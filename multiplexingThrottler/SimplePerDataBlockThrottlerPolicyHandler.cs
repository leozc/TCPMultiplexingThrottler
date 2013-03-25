using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using C5;
namespace multiplexingThrottler
{
   /**
    * Dummy Sleep timerbased throttler, does nothing good but testing
    */
    public class SimplePerDataBlockThrottlerPolicyHandler : IThrottlerPoclicyHandler
    {
        // contention point...
        private IPriorityQueue<IDeviceManager> todoqueue;
        private Thread worker;
        private int _sleepInterval = 200;
        public static readonly object locker = new object();

        public SimplePerDataBlockThrottlerPolicyHandler()
        {

           todoqueue = new IntervalHeap<IDeviceManager>();
           var work = new ThreadStart(ClawTodo);
           worker = new Thread(work);
           worker.Start();
        }

       public void DispatchOneDataCycle(IDeviceManager dm)
       {
           // Convert the string data to byte data using ASCII encoding.
           ////// get the number of future block
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
               int byteTransferred = dm.Client.EndSend(deviceManager);

               var ts = dm.Metrics.CurrentTick - dm.Metrics.LastTick;
               if (ts > dm.TimeBlockinMs * DeviceMetric.TICKPERMS)
                   DispatchOneDataCycle(dm);
               else
               {
                   lock (locker)
                   {
                       PutInQueue(dm);   
                   }
               }

           }
           catch (Exception e)
           {
               Console.WriteLine(e.ToString());
           }
        }

        /// <summary>
        /// Simple polling loop.
        /// Can use semaphore to optimize here
        /// </summary>
        private void ClawTodo()
        {
            while (true){
                IDeviceManager dm = null;
                lock (locker)
                {
                    try
                    {
                        dm = todoqueue.DeleteMin();
                    }
                    catch (NoSuchItemException e)
                    {
                    }
                }

                if (dm != null && dm.Metrics.LastTick + dm.TimeBlockinMs * DeviceMetric.TICKPERMS <= dm.Metrics.CurrentTick) //due
                {
                    DispatchOneDataCycle(dm);
                }
                else
                {
                    if (dm != null) { lock (locker) { todoqueue.Add(dm); } }
                    Thread.Sleep(_sleepInterval);
                }
            }
        }
        private void PutInQueue(DeviceManager dm)
        {
            todoqueue.Add(dm);
        }
   }
}
