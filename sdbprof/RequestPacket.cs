using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public abstract class RequestPacket : Packet
    {
        public byte commandSet;
        public byte command;
    }
}
