using System;
using System.IO;

namespace sdbprof
{
    public abstract class Packet
    {
        public UInt32 id;
        public byte flags;
        protected byte[] extraData;

        public virtual void ToStream(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}