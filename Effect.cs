using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sniper.Lighting.DMX
{
    public class Effect : IDisposable
    {
        public int Channel;
        public byte OriginalValue;
        public byte NewValue;
        public int Duration;
        public EasingType EasingTypeIn;
        public EasingType EasingTypeOut;
        public EasingExtents Extents;
        public object UserData;
        public Guid UniqueIdentifier;
        //public Sniper.Lighting.DMX.DMXProUSB.EaseDMXValueComplete OnComplete;
        //public Sniper.Lighting.DMX.DMXProUSB.EaseDMXValueStep OnStep;
        //internal Sniper.Lighting.DMX.DMXProUSB.InternalEaseDMXValueComplete InternalOnComplete;
        public event EventHandler<EffectEventArgs> OnStep;
        public event EventHandler<EffectEventArgs> OnStart;
        public event EventHandler<EffectEventArgs> OnComplete;
        public DateTime FromTimestamp;
        public DateTime ToTimestamp;
        private TimeSpan duration;
        private int steps;
        private int currentStep;
        public bool Running { get { return running;  } }
        private bool running;

        //private Thread thread;
        internal Effect(int Channel, byte OriginalValue, byte NewValue, int Duration, EasingType EasingTypeIn, EasingType EasingTypeOut, EasingExtents Extents)
        {
            this.Channel = Channel;
            this.OriginalValue = OriginalValue;
            this.NewValue = NewValue;
            this.Duration = Duration;
            this.EasingTypeIn = EasingTypeIn;
            this.EasingTypeOut = EasingTypeOut;
            this.Extents = Extents;
            //this.OnComplete = OnComplete;
            //this.OnStep = OnStep;
            //this.InternalOnComplete = InternalOnComplete;
            //thread = new Thread(new ParameterizedThreadStart(ThreadAction));
            this.UniqueIdentifier = Guid.NewGuid();
        }
        public void StartIn(int delay)
        {
            var handle = this;
            FromTimestamp = DateTime.Now.AddMilliseconds(delay);
            ToTimestamp = FromTimestamp.AddMilliseconds(handle.Duration);
            duration = ToTimestamp - FromTimestamp;
            steps = 255;
            currentStep = 0;
            running = true;
            if (handle.OnStart != null)
            {
                handle.OnStart(this, new EffectEventArgs()
                {
                    Channel = handle.Channel,
                    Direction = handle.OriginalValue > handle.NewValue ? -1 : 1,
                    Step = currentStep,
                    Value = (byte)handle.OriginalValue
                });
            }
        }
        public void Start()
        {
            StartIn(0);
        }
        public byte GetCurrentValue()
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

                if (DateTime.Now < this.ToTimestamp && fraction != 1)
                {
                    if (currentValueRounded != this.NewValue)
                    {
                        if (this.OnStep != null)
                        {
                            this.OnStep(this, new EffectEventArgs()
                            {
                                Channel = this.Channel,
                                Direction = this.OriginalValue > currentValueRounded ? -1 : 1,
                                Step = currentStep,
                                Value = (byte)currentValueRounded
                            });
                        }
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

                    running = false;
                    if (this.OnComplete != null)
                    {
                        this.OnComplete(this, new EffectEventArgs()
                        {
                            Channel = this.Channel,
                            Direction = this.OriginalValue > (byte)this.NewValue ? -1 : 1,
                            Step = currentStep,
                            Value = (byte)this.NewValue
                        });
                    }

                    return this.NewValue;
                }
            }
            else
            {
                return this.NewValue;
            }
        }
        
        public void Stop()
        {
            Stop(false);
        }
        public void Stop(bool resetToOriginal)
        {
            running = false; 
            if (this.Channel == 0) Console.WriteLine("Stop called for channel 0");
            var handle = this;
            //make sure we reached the final value
            DMXProUSB.SetDmxValue(this.Channel, (byte)handle.NewValue);
           
            if (resetToOriginal)
            {
                DMXProUSB.SetDmxValue(Channel, OriginalValue);
            }
            
            if (handle.OnComplete != null)
            {
              
                handle.OnComplete(this, new EffectEventArgs()
                {
                    Channel = handle.Channel,
                    Direction = handle.OriginalValue > (byte)handle.NewValue ? -1 : 1,
                    Step = currentStep,
                    Value = (byte)handle.NewValue
                });
            }           
        }

        #region IDisposable Members

        public void Dispose()
        {
            this.Stop(false);
        }

        #endregion
    }
}
