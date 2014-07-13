using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sniper.Lighting.DMX
{
    
    public class DmxControllerInstance
    {
        private List<Channel> Channels = new List<Channel>();
        private int _numChannels = 512;
        private Thread runningThread;
        private bool done;
        private DMXProUSB dmxDevice;

        public DmxControllerInstance(DMXDeviceType t)
        {
            dmxDevice = t == DMXDeviceType.ProDMX ? new DMXProUSB() : new OpenDMXUSB();
            done = false;
            lock (Channels)
            {
                for (int i = 0; i < _numChannels; i++)
                {
                    Channel c = new Channel(i, 
                        (int channel, byte value) => {
                            //Console.WriteLine("{0}: DMXProUSB.SetDmxValue({1},{2})", DateTime.Now, channel, value);
                            dmxDevice.SetDmxValue(channel, value);
                        });
                    Channels.Add(c);
                }
            }
            runningThread = new Thread(new ThreadStart(Run));
            runningThread.IsBackground = true;
            runningThread.Start();
            dmxDevice.StateChanged += new StateChangedEventHandler(DMXProUSB_StateChanged);
        }

        void DMXProUSB_StateChanged(object sender, StateChangedEventArgs e)
        {
            if (StateChanged != null)
            {
                StateChanged(sender, e);
            }
        }

        public Effect EaseDmxValue(int channel, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents)
        {
            Effect handle = new Effect(channel, Channels[channel].Value, endValue, duration, typeIn, typeOut, extents);
            Channels[channel].QueueEffect(handle);
            handle.Start();
            return handle;
        }

        public Effect EaseDmxValue(int channel, byte startValue, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents)
        {
            Effect handle = new Effect(channel, startValue, endValue, duration, typeIn, typeOut, extents);
            Channels[channel].QueueEffect(handle);
            handle.Start();
            return handle;
        }

        public Effect EaseDmxValue(int channel, byte startValue, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents, DateTime when)
        {
            Effect handle = new Effect(channel, startValue, endValue, duration, typeIn, typeOut, extents);
            Channels[channel].QueueEffect(handle);
            handle.StartIn((int)(when - DateTime.Now).TotalMilliseconds);            
            return handle;
        }

        public void SetDmxValue(int channel, byte value, DateTime when)
        {
            Effect handle = new Effect(channel, Channels[channel].Value, value, 0, EasingType.Linear, EasingType.Linear, EasingExtents.EaseInOut);
            Channels[channel].QueueEffect(handle);
            handle.StartIn((int)(when - DateTime.Now).TotalMilliseconds);          
        }

        public void SetDmxValue(int channel, byte value)
        {
            SetDmxValue(channel, value, DateTime.Now);
        }

        public void SetDmxValue(int channel, byte value, int revertInDuration)
        {
            byte current = GetDmxValue(channel);
            SetDmxValue(channel, value);
            SetDmxValue(channel, current, DateTime.Now.AddMilliseconds(revertInDuration));
        }

        public Effect PulseDmxValue(int channel, byte startValue, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents)
        {
            var handle = new Pulse(channel, startValue, endValue, duration, typeIn, typeOut, extents);
            Channels[channel].QueueEffect(handle);
            handle.Start();
            return handle;
        }

        public byte GetDmxValue(int channel)
        {
            return dmxDevice.GetDmxValue(channel);
        }        

        private void Run()
        {
            while (!done)
            {
                lock (Channels)
                {
                    foreach (Channel c in Channels)
                    {
                        try
                        {
                            c.Tick();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
                Thread.Sleep(5);
            }
        }

        public void Dispose()
        {
            done = true;
            Thread.Sleep(500);
            if (runningThread.ThreadState == ThreadState.Running)
            {
                runningThread.Abort();
            }
            runningThread = null;

            dmxDevice.Dispose();
        }

        public byte[] GetCurrentValues()
        {
            return dmxDevice.GetCurrentBuffer();
        }

        public bool Start()
        {
            return dmxDevice.start();
        }

        public bool Connected { get { return dmxDevice.Connected; } }
        public void SetLimits(DmxLimits newLimits)
        {
            dmxDevice.setLimits(newLimits);
        }

        public event StateChangedEventHandler StateChanged;


        public void Stop()
        {
            foreach ( var c in Channels )
            {
                c.Stop();
            }
            dmxDevice.stop();
        }
    }
}
