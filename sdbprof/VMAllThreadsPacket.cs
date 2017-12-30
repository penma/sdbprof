using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class VMAllThreadsRequest : IRequestPacket
    {
        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.VM, (byte)CmdVM.ALL_THREADS);
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new VMAllThreadsReply(replyFrame);
        }
    }

    public class VMAllThreadsReply : IReplyPacket
    {
        public UInt32[] threadObjectIds;

        public VMAllThreadsReply(ReplyFrame replyFrame)
        {
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                UInt32 len = ms.Read32BE();
                threadObjectIds = new UInt32[len];
                for (int i = 0; i < len; i++)
                {
                    threadObjectIds[i] = ms.Read32BE();
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("VM All Thread info: ");
            for (int i = 0; i < threadObjectIds.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.AppendFormat("[{0}] {1}", i, threadObjectIds[i]);
            }
            return sb.ToString();
        }
    }
}
