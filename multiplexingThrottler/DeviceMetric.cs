using System;

namespace multiplexingThrottler
{
    public class DeviceMetric
    {
        public const int  TICKPERMS = 10000;
        /***** properties *****/
        public long CurrentTick
        {
            get { return DateTime.Now.Ticks; }
        }

        /**
         * Report the current bPS so far
         */
        public long CurrentBitPerSecond
        {
            get {
                if (LastTick - StartTick < TICKPERMS * 5) // avoid 0 or overflow
                    return 0;
                else
                    return (long)(ByteSend * 8 / ((LastTick - StartTick) / TICKPERMS / 1000.0));
            }
        }

        /** the TS the fist data pack sent **/
       
        public long StartTick { get; set; }

        /** the TS for the last data pack received **/
        public long LastTick { get; set; }

        /** the The total byte sent so far **/
        public long ByteSend { get; set; }


        /** the total size of the buffer needs to deliver in byte **/
        public long TotalByte { get; set; }

        public override string ToString()
        {
            return String.Format("DeviceMetrics:StartTick={0}:LastTick={1}:ByteSend={2}:TotalByte={3}:CurrentBitPerSecond={4}",
                StartTick, LastTick, ByteSend, TotalByte, CurrentBitPerSecond);
        }
    }
}