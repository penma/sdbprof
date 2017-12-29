using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    class Unpack
    {
        public static UInt64 UInt64(byte[] buf, int offset = 0)
        {
            return ((UInt64)UInt32(buf, offset + 0) << 32) | UInt64(buf, offset + 4);
        }

        public static UInt32 UInt32(byte[] buf, int offset = 0)
        {
            return (UInt32)((buf[offset + 0] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | (buf[offset + 3] << 0));
        }

        public static UInt16 UInt16(byte[] buf, int offset = 0)
        {
            return (UInt16)((buf[offset + 0] << 8) | (buf[offset + 1] << 0));
        }

        public static byte[] String(byte[] buf, int offset = 0)
        {
            byte[] ret = new byte[UInt32(buf, offset)];
            Array.Copy(buf, 4, ret, 0, ret.Length);
            return ret;
        }
    }
}
