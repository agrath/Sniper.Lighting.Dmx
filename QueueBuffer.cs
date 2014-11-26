using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public class QueueBuffer
    {
        public QueueBuffer(int BusLength)
        {
            CurrentPriority = 1;
            Buffer = new byte[BusLength];
        }
        public int CurrentPriority
        {
            get;
            set;
        }
    
        public byte[] Buffer
        {
            get;
            set;
        }
    }
}
