using System;
using System.IO;
using System.Text;

namespace sdbprof
{
    static class PackUnpack
    {
        public static void Write64BE(this Stream s, UInt64 v)
        {
            s.Write32BE((UInt32)(v >> 32));
            s.Write32BE((UInt32)v);
        }

        public static void Write32BE(this Stream s, UInt32 v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)(v >> 0));
        }

        public static void Write16BE(this Stream s, UInt32 v)
        {
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)(v >> 0));
        }

        public static UInt64 Read64BE(this Stream s)
        {
            UInt32 hi = s.Read32BE();
            UInt32 lo = s.Read32BE();
            return ((UInt64)hi << 32) | lo;
        }

        public static UInt32 Read32BE(this Stream s)
        {
            byte[] buf = new byte[4];
            s.Read(buf, 0, 4);
            return (UInt32)((buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | (buf[3] << 0));
        }

        public static UInt16 Read16BE(this Stream s)
        {
            byte[] buf = new byte[2];
            s.Read(buf, 0, 2);
            return (UInt16)((buf[0] << 8) | (buf[1] << 0));
        }

        public static byte[] ReadBytebuf(this Stream s)
        {
            byte[] ret = new byte[s.Read32BE()];
            s.Read(ret, 0, ret.Length);
            return ret;
        }

        public static string ReadUTF8String(this Stream s)
        {
            return Encoding.UTF8.GetString(s.ReadBytebuf());
        }
    }
}
