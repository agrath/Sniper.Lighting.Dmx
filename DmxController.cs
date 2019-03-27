using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sniper.Lighting.DMX
{

    public static class DmxController<T> where T : DMXProUSB, new()
    {
        private static ThreadSafeList<Effect> effectQueue = new ThreadSafeList<Effect>();
        private static Thread runningThread;
        private static bool done;
        private static T dmxDevice;
        private readonly static int busLength = 512;

        static DmxController()
        {
            dmxDevice = new T();
            done = false;
            for (int i = 0; i < busLength; i++)
            {
                byte value = dmxDevice.getDefaultForChannel(i);
                dmxDevice.SetDmxValue(i, value, Guid.Empty, 1);
            }
            runningThread = new Thread(new ThreadStart(Run));
            runningThread.IsBackground = true;
            runningThread.Start();
            dmxDevice.StateChanged += new StateChangedEventHandler(DMXProUSB_StateChanged);
        }

        public static void QueueEffect(Effect e)
        {
            //create a lighting queue for this effect
            dmxDevice.CreateQueue(e.Queue, e.Priority);
            effectQueue.Add(e);
        }

        public static byte GetDefaultForChannel(int channel)
        {
            return dmxDevice.getDefaultForChannel(channel);
        }

        static void DMXProUSB_StateChanged(object sender, StateChangedEventArgs e)
        {
            if (StateChanged != null)
            {
                StateChanged(sender, e);
            }
        }

        public static Effect EaseDmxValue(Guid queue, int channel, int priority, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents)
        {
            byte startValue = dmxDevice.GetDmxValue(channel);
            Effect handle = new Effect(queue, channel, priority, startValue, endValue, duration, typeIn, typeOut, extents);
            QueueEffect(handle);
            handle.Start();
            return handle;
        }

        public static void SetDefaults(DmxDefaults newDefaults)
        {
            dmxDevice.setDefaults(newDefaults);
        }

        public static Effect EaseDmxValue(Guid queue, int channel, int priority, byte startValue, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents)
        {
            Effect handle = new Effect(queue, channel, priority, startValue, endValue, duration, typeIn, typeOut, extents);
            QueueEffect(handle);
            handle.Start();
            return handle;
        }

        public static Effect EaseDmxValue(Guid queue, int channel, int priority, byte startValue, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents, DateTime when)
        {
            Effect handle = new Effect(queue, channel, priority, startValue, endValue, duration, typeIn, typeOut, extents);
            QueueEffect(handle);
            handle.StartIn((int)(when - DateTime.Now).TotalMilliseconds);
            return handle;
        }

        public static void SetDmxValue(Guid queue, int channel, int priority, byte value, DateTime when)
        {
            Effect handle = new Effect(queue, channel, priority, null, value, 0, EasingType.Linear, EasingType.Linear, EasingExtents.EaseInOut);
            QueueEffect(handle);
            handle.StartIn((int)(when - DateTime.Now).TotalMilliseconds);
        }

        public static void SetDmxValue(Guid queue, int channel, int priority, byte value)
        {
            SetDmxValue(queue, channel, priority, value, DateTime.Now);
        }

        public static void SetDmxValue(Guid queue, int channel, int priority, byte value, int revertInDuration)
        {
            var currentValue = GetDmxValue(channel);
            SetDmxValue(queue, channel, priority, value);
            if (revertInDuration != 0)
            {
                SetDmxValue(queue, channel, priority, currentValue, DateTime.Now.AddMilliseconds(revertInDuration));
            }
        }

        public static void SetDmxValue(Guid queue, int channel, int priority, byte value, int revertInDuration, int delayDuration)
        {
            var currentValue = GetDmxValue(channel);
            SetDmxValue(queue, channel, priority, value, DateTime.Now.AddMilliseconds(delayDuration));
            if (revertInDuration != 0)
            {
                SetDmxValue(queue, channel, priority, currentValue, DateTime.Now.AddMilliseconds(delayDuration).AddMilliseconds(revertInDuration));
            }
        }

        public static Effect PulseDmxValue(Guid queue, int channel, int priority, byte startValue, byte endValue, int duration, EasingType typeIn, EasingType typeOut, EasingExtents extents)
        {
            var handle = new Pulse(queue, channel, priority, startValue, endValue, duration, typeIn, typeOut, extents);
            QueueEffect(handle);
            handle.Start();
            return handle;
        }
        public static Effect HoldDmxValue(Guid queue, int channel, int priority, byte newValue, byte revertValue, int duration, int delayDuration)
        {
            var handle = new Hold(queue, channel, priority, newValue, revertValue, duration);
            QueueEffect(handle);
            handle.StartIn(delayDuration);
            return handle;
        }
        public static byte GetDmxValue(int channel)
        {
            return dmxDevice.GetDmxValue(channel);
        }

        public static void ClearFutureQueueForDmxChannel(Guid queue, int channel)
        {
            Queue<Effect> toRemove = new Queue<Effect>();
            DateTime moment = DateTime.Now;

            foreach (Effect e in effectQueue)
            {
                if (e.Queue == queue && e.Channel == channel && e.FromTimestamp > moment)
                {
                    toRemove.Enqueue(e);
                }
            }

            while (toRemove.Count > 0)
            {
                Effect e = toRemove.Dequeue();
                effectQueue.Remove(e);
            }

        }

        public static void ClearEntireQueueForDmxChannel(Guid queue, int channel)
        {
            Queue<Effect> toRemove = new Queue<Effect>();
            DateTime moment = DateTime.Now;

            foreach (Effect e in effectQueue)
            {
                if (e.Queue == queue && e.Channel == channel)
                {
                    toRemove.Enqueue(e);
                }
            }

            while (toRemove.Count > 0)
            {
                Effect e = toRemove.Dequeue();
                effectQueue.Remove(e);
            }

        }
        private static void Run()
        {
            Queue<Effect> toRemove = new Queue<Effect>();
            while (!done)
            {
                int activeEffectsCount = effectQueue.Count;
                DateTime moment = DateTime.Now;

                foreach (Effect e in effectQueue)
                {
                    if (e.FromTimestamp <= moment) //don't check the upper bound otherwise if it's an instant effect, it's already passed by the time this is evaluated
                    {
                        byte value = e.GetCurrentValue();
                        Guid queue = e.Queue;
                        int channel = e.Channel;
                        int priority = e.Priority; //it's our pie-ority
                        dmxDevice.SetDmxValue(channel, value, queue, priority);
                    }
                    if (moment > e.ToTimestamp)
                    {
                        toRemove.Enqueue(e);
                    }
                }
                while (toRemove.Count > 0)
                {
                    Effect finishedEffect = toRemove.Dequeue();
                    effectQueue.Remove(finishedEffect);
                }
                var activeQueues = effectQueue.Select(x => x.Queue).Distinct();
                var currentQueues = dmxDevice.GetCurrentQueueIds();
                var inactiveQueues = currentQueues.Except(activeQueues);
                foreach (var queue in inactiveQueues)
                {
                    if (queue != Guid.Empty) //never delete empty guid queue, used for test UI
                    {
                        dmxDevice.DeleteQueue(queue);
                    }
                }
                Thread.Sleep(5);
            }
        }

        public static void Dispose()
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

        public static byte[] GetCurrentValues()
        {
            return dmxDevice.GetCurrentBuffer();
        }

        public static bool Start()
        {
            return dmxDevice.start();
        }

        public static bool Connected { get { return dmxDevice.Connected; } }
        public static void SetLimits(DmxLimits newLimits)
        {
            dmxDevice.setLimits(newLimits);
        }

        public static event StateChangedEventHandler StateChanged;


        public static void Stop()
        {
            foreach (Effect e in effectQueue)
            {
                e.Stop();
            }
            dmxDevice.stop();
        }
    }
}
