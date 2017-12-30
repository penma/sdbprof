using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class MethodGetDeclaringTypeRequest : IRequestPacket
    {
        public UInt32 methodId;

        public MethodGetDeclaringTypeRequest(UInt32 methodId)
        {
            this.methodId = methodId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.METHOD, (byte)CmdMethod.GET_DECLARING_TYPE, PackUnpack.Pack32BE(methodId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new MethodGetDeclaringTypeReply(replyFrame, methodId);
        }
    }

    public class MethodGetDeclaringTypeReply : IReplyPacket
    {
        public UInt32 methodId;
        public UInt32 typeId;

        public MethodGetDeclaringTypeReply(ReplyFrame replyFrame, UInt32 methodId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("MethodGetDeclaringTypeReply(" + methodId + ") error code " + replyFrame.errorCode);
            }
            this.methodId = methodId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                typeId = ms.Read32BE();
            }
        }
    }
}
