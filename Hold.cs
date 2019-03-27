using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public class Hold : Effect
    {
        //private Thread thread;
        internal Hold(Guid Queue, int Channel, int Priority, byte NewValue, byte RevertValue, int Duration) :
            base(Queue, Channel, Priority, RevertValue, NewValue, Duration, EasingType.Linear, EasingType.Linear, EasingExtents.EaseInOut)
        {
        }

        public override byte GetCurrentValue()
        {
            if (running)
            {
                TimeSpan elapsed = (DateTime.Now - FromTimestamp);
                return this.NewValue;
            }
            return this.OriginalValue;
        }
    }
}
