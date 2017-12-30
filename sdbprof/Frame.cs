using System;
using System.IO;

namespace sdbprof
{
    public abstract class Frame
    {
        public UInt32 id;
        public byte flags;
        public byte[] extraData;

        public static Frame ReadFromStream(Stream stream)
        {
            Frame frame;

            int len = (int)stream.Read32BE() - 11;
            UInt32 id = stream.Read32BE();
            byte flags = (byte)stream.ReadByte();

            if ((flags & 0x80) != 0)
            {
                frame = new ReplyFrame();
                (frame as ReplyFrame).errorCode = (ErrorCode)stream.Read16BE();
            }
            else
            {
                CommandSet set = (CommandSet)stream.ReadByte();
                byte cmd = (byte)stream.ReadByte();
                frame = new RequestFrame(set, cmd);
            }

            frame.id = id;
            frame.extraData = new byte[len];
            stream.Read(frame.extraData, 0, len);

            return frame;
        }
    }
}