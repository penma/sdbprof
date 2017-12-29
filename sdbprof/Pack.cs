using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    static class Pack
    {
        public static void Write64BE(this Stream ms, UInt64 v)
        {
            ms.Write32BE((UInt32)(v >> 32));
            ms.Write32BE((UInt32)v);
        }

        public static void Write32BE(this Stream ms, UInt32 v)
        {
            ms.WriteByte((byte)(v >> 24));
            ms.WriteByte((byte)(v >> 16));
            ms.WriteByte((byte)(v >> 8));
            ms.WriteByte((byte)(v >> 0));
        }

        public static void Write16BE(this Stream ms, UInt32 v)
        {
            ms.WriteByte((byte)(v >> 8));
            ms.WriteByte((byte)(v >> 0));
        }
    }
}
