using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using C5;
using System.Diagnostics;
namespace multiplexingThrottler
{
   /**
    * Simple constant rate throttler, no burst 
    */
    public class SimplePerDataBlockThrottlerPolicyHandler : IThrottlerPoclicyHandler
    {
        // contention point...
        private IPriorityQueue<IDeviceManager> todoqueue;
        private Thread worker;
        private int _sleepInterval = 100;
        public static readonly object locker = new object();

        public SimplePerDataBlockThrottlerPolicyHandler()
        {

           todoqueue = new IntervalHeap<IDeviceManager>();
           var work = new ThreadStart(ClawTodo);
           worker = new Thread(work);
           worker.Start();
        }

        public virtual void DispatchOneDataCycle(IDeviceManager dm)
       {
           IAsyncResult r = dm.DeliveryNextBlockOfData(SendCompleteHandler);
           if (r == null)
               Console.WriteLine(dm.Ipaddr.ToString() + " completed");
          
       }
        public virtual void SendCompleteHandler(IAsyncResult deviceManager)
       {
            Stopwatch stopwatch = new Stopwatch();

           try
           {
               var dm = deviceManager.AsyncState as IDeviceManager;
               if (dm == null)
                   throw new ArgumentException("Hey what is wrong here? The DispatchOneDataCycle put in wrong arg??? Found: "+deviceManager.GetType());
               
               int byteSent = dm.CompleteOneDataCycle(deviceManager); //must call this
               var ts = dm.Metrics.CurrentTick - dm.Metrics.LastTick;

               if (IfProceedToNextDataCycle(dm))
               {
                   DispatchOneDataCycle(dm);
               }
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
           stopwatch.Stop();
        }

        protected static bool IfProceedToNextDataCycle(IDeviceManager dm)
        {
            return (dm.ExpectedByteSent >= dm.ContentSizeForOperate) ||
                                 dm.ExpectedByteSent - dm.Metrics.ByteSent >= dm.SpeedInBytePerTimeBlock;
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
                    catch (NoSuchItemException)
                    {
                    }
                }

                if (dm != null && IfProceedToNextDataCycle(dm)) //due
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
        private void PutInQueue(IDeviceManager dm)
        {
            todoqueue.Add(dm);
        }
   }
}
