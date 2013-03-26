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
    * Dummy Sleep timerbased throttler, does nothing good but testing
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
           // Convert the string data to byte data using ASCII encoding.
           ////// get the number of future block
 
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
                                 dm.ExpectedByteSent - dm.Metrics.ByteSend >= dm.SpeedInBytePerTimeBlock;
        }

        /// <summary>
        /// Simple polling loop.
        /// Can use semaphore to optimize here
        /// </summary>
        private void ClawTodo()
        {
            
            while (true){
                Stopwatch s = new Stopwatch();
                s.Start();
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
                //Console.WriteLine("ClawTodo:unlock =" + s.ElapsedTicks);

                if (dm != null && IfProceedToNextDataCycle(dm)) //due
                {
                    DispatchOneDataCycle(dm);
                    //Console.WriteLine("ClawTodo:dispached =" + s.ElapsedTicks);
                }
                else
                {
                    if (dm != null) { lock (locker) { todoqueue.Add(dm); } }
                    Thread.Sleep(_sleepInterval);
                }
                s.Stop();

                //if (dm != null)
                //{
                //    Console.WriteLine(dm);
                //    Console.WriteLine(dm.Metrics);
                //}

            }
        }
        private void PutInQueue(IDeviceManager dm)
        {
            todoqueue.Add(dm);
        }
   }
}
