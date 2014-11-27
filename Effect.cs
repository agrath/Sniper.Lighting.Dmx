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
        public int Priority;
        protected byte OriginalValue;
        public byte NewValue;
        protected int Duration;
        protected EasingType EasingTypeIn;
        protected EasingType EasingTypeOut;
        protected EasingExtents Extents;
        protected object UserData;
        protected Guid UniqueIdentifier;
        //public Sniper.Lighting.DMX.DMXProUSB.EaseDMXValueComplete OnComplete;
        //public Sniper.Lighting.DMX.DMXProUSB.EaseDMXValueStep OnStep;
        //internal Sniper.Lighting.DMX.DMXProUSB.InternalEaseDMXValueComplete InternalOnComplete;
        public event EventHandler<EffectEventArgs> Step;
        public event EventHandler<EffectEventArgs> Starting;
        public event EventHandler<EffectEventArgs> Complete;
        public DateTime FromTimestamp;
        public DateTime ToTimestamp;
        protected TimeSpan duration;
        protected int steps;
        protected int currentStep;
        public Guid Queue;
        public bool Running { get { return running;  } }
        protected bool running;

        protected virtual void OnStep(EffectEventArgs e)
        {
            EventHandler<EffectEventArgs> handler = Step;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnStart(EffectEventArgs e)
        {
            EventHandler<EffectEventArgs> handler = Starting;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnComplete(EffectEventArgs e)
        {
            EventHandler<EffectEventArgs> handler = Complete;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        //private Thread thread;
        internal Effect(Guid queue, int Channel, int Priority, byte? OriginalValue, byte NewValue, int Duration, EasingType EasingTypeIn, EasingType EasingTypeOut, EasingExtents Extents)
        {
            this.Queue = queue;
            this.Priority = Priority;
            this.Channel = Channel;
            //disable revert as multiple effects on same channels simultaneously cause bugs, if you want a light to revert to a non off state, have an effect which never ends
            if (OriginalValue != null)
            {
                this.OriginalValue = OriginalValue.Value;
            }
            else
            {
                this.OriginalValue = 0;
            }
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
            ToTimestamp = FromTimestamp.Add(TimeSpan.FromMilliseconds(Duration));
            duration = ToTimestamp - FromTimestamp;
            steps = 255;
            currentStep = 0;
            running = true;
            if (handle.Starting != null)
            {
                handle.Starting(this, new EffectEventArgs()
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

        public virtual byte GetCurrentValue()
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
                        if (this.Step != null)
                        {
                            this.Step(this, new EffectEventArgs()
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
                    if (this.Complete != null)
                    {
                        this.Complete(this, new EffectEventArgs()
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
            //DMXProUSB.SetDmxValue(this.Channel, (byte)handle.NewValue);
            NewValue = 0;

            if (resetToOriginal)
            {
                NewValue = OriginalValue;
                //DMXProUSB.SetDmxValue(Channel, OriginalValue);
            }
            
            if (handle.Complete != null)
            {
              
                handle.Complete(this, new EffectEventArgs()
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
