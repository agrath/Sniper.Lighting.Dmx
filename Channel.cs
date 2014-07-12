using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    class Channel
    {
        public int Id { get; set; }
        public byte Value { get; set; }
        public List<Effect> Effects { get; set; }
        private Action<int, byte> Set;

        public Channel(int id, Action<int, byte> setValueAction)
        {
            this.Id = id;
            this.Set = setValueAction; // use this outside the channel to update the main buffer
            this.Effects = new List<Effect>();
        }

        public void QueueEffect(Effect e)
        {
            lock (Effects)
            {
                Effects.Add(e);
            }
        }

        public void Tick()
        {
            lock (Effects)
            {
                if (Effects.Count == 0) return;

                List<Effect> toRemove = new List<Effect>();

                int sum = 0;
                bool hasValue = false;
                foreach (Effect e in Effects)
                {
                    if (!e.Running)
                    {
                        continue;
                    }

                    DateTime moment = DateTime.Now;

                    byte value = e.GetCurrentValue();

                    if (moment >= e.FromTimestamp && moment <= e.ToTimestamp)
                    {
                        sum += value;
                        //Console.WriteLine("{0}: Effect {1} produced value {2} for channel {3}", DateTime.Now, e.UniqueIdentifier, value, e.Channel);
                        hasValue = true;
                    }

                    if (moment > e.ToTimestamp)
                    {
                        sum += e.NewValue;
                        toRemove.Add(e);

                        //Console.WriteLine("{0}: Effect {1} produced value {2} for channel {3} (Finalize)", DateTime.Now, e.UniqueIdentifier, e.NewValue, e.Channel);

                        hasValue = true;
                    }
                }

                Effects.RemoveAll(x => toRemove.Contains(x));

                if (hasValue)
                {
                    if (sum > 255)
                    {
                        sum = 255;
                    }
                    if (sum < 0)
                    {
                        sum = 0;
                    }
                    Value = (byte)sum;
                    Set(this.Id, Value);
                }
               
            }
        }
        
    }
}
