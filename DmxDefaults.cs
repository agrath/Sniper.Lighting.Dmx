using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sniper.Lighting.DMX.Properties;

namespace Sniper.Lighting.DMX
{
    public class DmxDefaults
    {
        public byte[] Values;
        public DmxDefaults()
        {
            Values = new byte[Settings.Default.DMXChannelCount];
        }
    }
}
