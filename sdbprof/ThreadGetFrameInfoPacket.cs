using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class ThreadGetFrameInfoRequest : IRequestPacket
    {
        public UInt32 threadId;

        public ThreadGetFrameInfoRequest(UInt32 threadId)
        {
            this.threadId = threadId;
        }

        public RequestFrame MakeRequestFrame()
        {
            using (MemoryStream ms = new MemoryStream(12))
            {
                ms.Write32BE(threadId);
                ms.Write32BE(0); // start
                ms.Write32BE(0xffffffff); // len -1
                return new RequestFrame(CommandSet.THREAD, (byte)CmdThread.GET_FRAME_INFO, ms.ToArray());
            }
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new ThreadGetFrameInfoReply(replyFrame, threadId);
        }
    }

    public class ThreadGetFrameInfoReply : IReplyPacket
    {
        public UInt32 threadId;

        public struct Frame
        {
            public UInt32 frameId;
            public UInt32 methodId;
            public UInt32 ilOffset;
            public StackFrameFlags flags;
        }
        public Frame[] frames;

        public ThreadGetFrameInfoReply(ReplyFrame replyFrame, UInt32 threadId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("ThreadGetFrameInfoReply(" + threadId + ") error code " + replyFrame.errorCode);
            }
            this.threadId = threadId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                frames = new Frame[ms.Read32BE()];
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i].frameId = ms.Read32BE();
                    frames[i].methodId = ms.Read32BE();
                    frames[i].ilOffset = ms.Read32BE();
                    frames[i].flags = (StackFrameFlags)ms.ReadByte();
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Thread ID {0} has {1} frames:", threadId, frames.Length);
            for (int i = 0; i < frames.Length; i++)
            {
                sb.AppendFormat("\n[{0}] frameId {1} methodId {2} ilOffset {3:X} flags {4}",
                    i, frames[i].frameId, frames[i].methodId, frames[i].ilOffset, frames[i].flags);
            }
            return sb.ToString();
        }
    }
}
