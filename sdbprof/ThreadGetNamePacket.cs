using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class ThreadGetNameRequest : IRequestPacket
    {
        public UInt32 threadId;

        public ThreadGetNameRequest(UInt32 threadId) {
            this.threadId = threadId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.THREAD, (byte)CmdThread.GET_NAME, PackUnpack.Pack32BE(threadId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new ThreadGetNameReply(replyFrame);
        }
    }

    public class ThreadGetNameReply : IReplyPacket
    {
        public String threadName;

        public ThreadGetNameReply(ReplyFrame replyFrame)
        {
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                threadName = ms.ReadUTF8String();
            }
        }
    }
}
