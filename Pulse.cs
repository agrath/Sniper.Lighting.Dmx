using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public class Pulse : Effect
    {
         //private Thread thread;
        internal Pulse(Guid Queue, int Channel, int Priority, byte OriginalValue, byte NewValue, int Duration, EasingType EasingTypeIn, EasingType EasingTypeOut, EasingExtents Extents) :
            base(Queue, Channel, Priority, OriginalValue, NewValue, Duration, EasingTypeIn, EasingTypeOut, Extents)
        {
        }

        public override byte GetCurrentValue()
        {
            if (running)
            {

                TimeSpan elapsed = (DateTime.Now - FromTimestamp);
                double fraction = (double)elapsed.Ticks / (double)duration.Ticks;
                if (fraction > 1)
                {
                    fraction = 1;
                }
                if (fraction < 0)
                {
                    fraction = 0;
                }
                currentStep = (int)Math.Ceiling(fraction * steps);

                float multiplier = 0;
                switch (this.Extents)
                {
                    case EasingExtents.EaseIn:
                        multiplier = Easing.EaseIn(((double)currentStep / (double)steps), this.EasingTypeIn);
                        break;
                    case EasingExtents.EaseInOut:
                        multiplier = Easing.EaseOut(((double)currentStep / (double)steps), this.EasingTypeOut);
                        break;
                    case EasingExtents.EaseOut:
                        multiplier = Easing.EaseInOut(((double)currentStep / (double)steps), this.EasingTypeIn, this.EasingTypeOut);
                        break;
                }
                int delta = (int)this.NewValue - (int)this.OriginalValue;
                float currentValue = ((float)(this.OriginalValue + ((double)delta * multiplier)));
                int currentValueRounded = (int)Math.Ceiling(currentValue);

                if (DateTime.Now < this.ToTimestamp && fraction != 1.0)
                {
                    if (currentValueRounded != this.NewValue)
                    {
                        
                        OnStep(new EffectEventArgs()
                        {
                            Channel = this.Channel,
                            Direction = this.OriginalValue > currentValueRounded ? -1 : 1,
                            Step = currentStep,
                            Value = (byte)currentValueRounded
                        });
                        //Console.WriteLine("{0}: Easing handle step {3}/{4} for channel {1} with value {2} ({5})", DateTime.Now, handle.Channel, currentValueRounded, currentStep, steps, (handle.OriginalValue > handle.NewValue ? "OUT" : "IN"));
                        //DMXProUSB.SetDmxValue(handle.Channel, (byte)currentValueRounded);

                        return (byte)currentValueRounded;
                    }

                    return this.NewValue;
                }
                else
                {
                    //make sure we reached the final value
                    //DMXProUSB.SetDmxValue(handle.Channel, (byte)handle.NewValue);

                    //Console.WriteLine("{0}: Easing handle completing for channel {1} with value {2} ({3})", DateTime.Now, handle.Channel, NewValue, (handle.OriginalValue > handle.NewValue ? "OUT" : "IN"));

                    var temp = NewValue;
                    NewValue = OriginalValue;
                    OriginalValue = temp;
                    var moment = DateTime.Now;
                    this.FromTimestamp = moment;
                    this.ToTimestamp = moment.Add(TimeSpan.FromMilliseconds(Duration));
                    return this.NewValue;
                }
            }
            else
            {
                return this.NewValue;
            }
        }
    }
}
