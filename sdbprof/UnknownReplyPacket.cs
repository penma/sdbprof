using System;
using System.IO;
using System.Text;

namespace sdbprof
{
    public class UnknownReplyPacket : ReplyPacket
    {
        public UnknownReplyPacket(uint id, byte flags, UInt16 errorCode, byte[] extraData)
        {
            this.id = id;
            this.flags = flags;
            this.errorCode = (ErrorCode) errorCode;
            this.extraData = extraData;
        }

        public override string ToString()
        {
            return String.Format("Reply id={0} errorcode={1} + {2} bytes of data <{3}>",
                id, errorCode, extraData.Length, SDB.DumpByteArray(extraData));
        }

        public override void ToStream(Stream stream)
        {
            /* TODO: Somehow share the header printing with the base */
            stream.Write32BE(11 + (UInt32)extraData.Length);
            stream.Write32BE(id);
            stream.WriteByte((byte)(flags | 0x80));
            stream.Write16BE((UInt16)errorCode);
            stream.Write(extraData, 0, extraData.Length);
        }
    }
}
