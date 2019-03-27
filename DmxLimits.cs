using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sniper.Lighting.DMX.Properties;

namespace Sniper.Lighting.DMX
{
    public class DmxLimits
    {
        public byte[] Min;
        public byte[] Max;
        public DmxLimits()
        {
            Min = new byte[Settings.Default.DMXChannelCount];
            Max = new byte[Settings.Default.DMXChannelCount];

            for (int i = 0; i < Max.Length; i++)
            {
                Max[i] = 255;
            }
        }
    }
}
