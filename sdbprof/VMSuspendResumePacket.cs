using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class VMSuspendRequest : IRequestPacket
    {
        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.VM, (byte)CmdVM.SUSPEND);
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new EmptyReply(replyFrame);
        }
    }

    public class VMResumeRequest : IRequestPacket
    {
        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.VM, (byte)CmdVM.RESUME);
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new EmptyReply(replyFrame);
        }
    }

    public class EmptyReply : IReplyPacket
    {
        public EmptyReply(ReplyFrame replyFrame)
        {
            if (replyFrame.extraData.Length != 0)
            {
                Console.WriteLine("unexpected non-empty reply frame: " + replyFrame.ToString());
            }
            this.errorCode = replyFrame.errorCode;
        }
    }
}
