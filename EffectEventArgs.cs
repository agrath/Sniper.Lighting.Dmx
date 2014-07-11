using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public class EffectEventArgs : EventArgs
    {
        public int Direction;
        public byte Value;
        public int Channel;
        public int Step;
    }
}
