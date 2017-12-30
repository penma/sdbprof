using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class DomGetAssembliesRequest : IRequestPacket
    {
        public UInt32 domainId;

        public DomGetAssembliesRequest(UInt32 domainId)
        {
            this.domainId = domainId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.APPDOMAIN, (byte)CmdAppDomain.GET_ASSEMBLIES, PackUnpack.Pack32BE(domainId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new DomGetAssembliesReply(replyFrame);
        }
    }

    public class DomGetAssembliesReply : IReplyPacket
    {
        public UInt32[] assemblyIds;

        public DomGetAssembliesReply(ReplyFrame replyFrame)
        {
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                UInt32 len = ms.Read32BE();
                assemblyIds = new UInt32[len];
                for (int i = 0; i < len; i++)
                {
                    assemblyIds[i] = ms.Read32BE();
                }
            }
        }
    }
}
