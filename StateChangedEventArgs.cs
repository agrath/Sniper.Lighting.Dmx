using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sniper.Lighting.DMX
{
    public class StateChangedEventArgs : EventArgs
    {
        public byte[] CurrentState;
    }
}
