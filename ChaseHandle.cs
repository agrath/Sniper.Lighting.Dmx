using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public class ChaseHandle
    {
        public List<int> Channels;
        public int TriggerDirection;
        public int TriggerValue;
        public int DurationPerStep;
        public int MaxRepeatCount;
        public int CurrentRepeatCount;
        public byte PulseValue;
        public int CurrentChannel;
        public ChaseHandle()
        {
            Channels = new List<int>();
        }
    }
}
