using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class MethodGetNameRequest : IRequestPacket
    {
        public UInt32 methodId;

        public MethodGetNameRequest(UInt32 methodId)
        {
            this.methodId = methodId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.METHOD, (byte)CmdMethod.GET_NAME, PackUnpack.Pack32BE(methodId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new MethodGetNameReply(replyFrame, methodId);
        }
    }

    public class MethodGetNameReply : IReplyPacket
    {
        public UInt32 methodId;
        public String methodName;

        public MethodGetNameReply(ReplyFrame replyFrame, UInt32 methodId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("MethodGetNameReply(" + methodId + ") error code " + replyFrame.errorCode);
            }
            this.methodId = methodId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                methodName = ms.ReadUTF8String();
            }
        }
    }
}
