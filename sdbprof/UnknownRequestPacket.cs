using System;
using System.IO;
using System.Text;

namespace sdbprof
{
    public class UnknownRequestPacket : RequestPacket
    {
        public UnknownRequestPacket(uint id, byte flags, byte commandSet, byte command, byte[] extraData)
        {
            this.id = id;
            this.flags = flags;
            this.commandSet = commandSet;
            this.command = command;
            this.extraData = extraData;
        }

        public override string ToString()
        {
            return String.Format("Request id={4} set={0}(0x{0:X2}) cmd={1}(0x{1:X2}) + {2} bytes of data <{3}>",
                commandSet, command, extraData.Length, SDB.DumpByteArray(extraData), id);
        }

        public override void ToStream(Stream stream)
        {
            /* TODO: Somehow share the header printing with the base */
            stream.Write32BE(11 + (UInt32)extraData.Length);
            stream.Write32BE(id);
            stream.WriteByte((byte)(flags & ~0x80));
            stream.WriteByte(commandSet);
            stream.WriteByte(command);
            stream.Write(extraData, 0, extraData.Length);
        }
    }
}
