using System;
using System.IO;

namespace sdbprof
{
    public class RequestFrame : Frame
    {
        public CommandSet commandSet;
        public byte command;

        public RequestFrame(CommandSet commandSet, byte command) : this(commandSet, command, new byte[0]) { }

        public RequestFrame(CommandSet commandSet, byte command, byte[] extraData)
        {
            this.commandSet = commandSet;
            this.command = command;
            this.extraData = extraData;
        }

        public void ToStream(Stream stream)
        {
            stream.Write32BE(11 + (UInt32)extraData.Length);
            stream.Write32BE(id);
            stream.WriteByte(0); // flags
            stream.WriteByte((byte) commandSet);
            stream.WriteByte(command);
            stream.Write(extraData, 0, extraData.Length);
        }

        public override String ToString()
        {
            return String.Format("Request id={0} set={1}({2}) cmd={3} + {4} bytes of data <{5}>",
                id,
                (byte)commandSet, commandSet.ToString(),
                command,
                extraData.Length, SDB.DumpByteArray(extraData)
                );
        }
    }
}
