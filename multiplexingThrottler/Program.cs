using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
namespace multiplexingThrottler
{
    public class Runner
    {
        /**
         * arg[0] is the config file
         * arg[2] is the throttler of choice
         */ 
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("exec SampleInput.txt SampleData");
                Environment.Exit(-1);
            }

            String filename = args[0];
            String content = args[1];
            Byte[] bytes = File.ReadAllBytes(content);
            var lines = File.ReadLines(filename);
            
            var ips = new List<String>(); // { "127.0.0.1:8000", "127.0.0.1:8001", "127.0.0.1:8002", "127.0.0.1:8003" };
            var bps = new List<int>();
            foreach (var l in lines)
            {
                var array = l.Split(new char[]{':'});
                ips.Add(array[0] + ":" + array[1]);
                //ips.Add("127.0.0.1" + ":" + array[1]);
               
                bps.Add(int.Parse(array[2]));
                //bps.Add(512*1024);
            }

            /** CHECK ASSUMPTION OF THE APPLICATION, THESE ASSERTION CAN BE REMOVED TO YIELD MORE FLEXIBLE APP **/
            Debug.Assert(bytes.Length == 1024*1024*64,"File must be exactly 64 MBs - 67108864 , use 'fsutil file createnew sampledata 67108864' to create "); // must be 64 bytes
            Debug.Assert(ips.Count == 64, "File must contain 64 IP based address"); 
            Debug.Assert(bps.Count == 64, "File must contain 64 IP based address");

            /** PICK ONE IMPLEMENTATION **/
            //var mt = new MultiplexThrottler<SimplePerDataBlockThrottlerPolicyHandler>(ips, bps, bytes);
            //var mt = new MultiplexThrottler<UnlimitedThrottlerPolicyHandler>(ips, bps, bytes);
            var mt = new MultiplexThrottler<CatchupThrottlerPolicyHandler>(ips, bps, bytes);

            try
            {
                mt.ConnectAllDevices();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.Exit(-1);
            }

            // start to deliver
            mt.DeliverAllDevices();
         
            /** CRAWLING, WAIT FOR RESULT ... **/
            for (int i = 0; i <= 100; i++)
            {
                bool allDone = true;
                foreach (var dm in mt.DeviceManagers)
                {
                    bool done = dm.SendCompleteSignal.WaitOne(1000);
                    if (done)
                        Console.WriteLine(dm.Ipaddr+":"+dm.Port + " is done");
                    else
                    {
                        allDone = false;
                        Console.WriteLine(
                            String.Format("DEVICE:{0}  CurrentSpeed(bps):{1} TimeSpent:{2}", dm, dm.Metrics.CurrentBitPerSecond, (dm.Metrics.LastTick - dm.Metrics.StartTick) / DeviceMetric.TICKPERMS));
                    
                    }
                }
                /**
                 * REPORT
                 */
                if (allDone)
                {
                    Console.WriteLine("ALL DONE!");
                    foreach (var dm in mt.DeviceManagers)
                    {
                        Console.WriteLine(
                            String.Format("COMPLETED=>DEVICE:{0}  CurrentSpeed(bps):{1} TimeSpent:{2}", dm, dm.Metrics.CurrentBitPerSecond, (dm.Metrics.LastTick - dm.Metrics.StartTick) / DeviceMetric.TICKPERMS));
                    }
                    Environment.Exit(0);
                }
                else
                    Thread.Sleep(5000);
            }
            Environment.Exit(-1); // error here
        }


    }
}
