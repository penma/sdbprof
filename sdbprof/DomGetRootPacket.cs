using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class DomGetRootRequest : IRequestPacket
    {
        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.APPDOMAIN, (byte)CmdAppDomain.GET_ROOT_DOMAIN);
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new DomGetRootReply(replyFrame);
        }
    }

    public class DomGetRootReply : IReplyPacket
    {
        public UInt32 domainId;

        public DomGetRootReply(ReplyFrame replyFrame)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("DomGetRootReply error code " + replyFrame.errorCode);
            }
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                domainId = ms.Read32BE();
            }
        }
    }
}
