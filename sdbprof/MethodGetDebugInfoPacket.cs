using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class MethodGetDebugInfoRequest : IRequestPacket
    {
        public UInt32 methodId;

        public MethodGetDebugInfoRequest(UInt32 methodId)
        {
            this.methodId = methodId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.METHOD, (byte)CmdMethod.GET_DEBUG_INFO, PackUnpack.Pack32BE(methodId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new MethodGetDebugInfoReply(replyFrame, methodId);
        }
    }

    public class MethodGetDebugInfoReply : IReplyPacket
    {
        public UInt32 methodId;
        public UInt32 codeSize;
        public String sourceFilename;
        public Dictionary<UInt32, UInt32> ilOffsetsToLineNumbers;

        public MethodGetDebugInfoReply(ReplyFrame replyFrame, UInt32 methodId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("MethodGetDebugInfoReply(" + methodId + ") error code " + replyFrame.errorCode);
            }
            this.methodId = methodId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                codeSize = ms.Read32BE();
                sourceFilename = ms.ReadUTF8String();
                int len = (int)ms.Read32BE();
                ilOffsetsToLineNumbers = new Dictionary<uint, uint>(len);
                for (int i = 0; i < len; i++)
                {
                    UInt32 ilOffset = ms.Read32BE();
                    UInt32 line = ms.Read32BE();
                    /* XXX what to do if the same offset is mapped to multiple lines (and yes this does happen) */
                    ilOffsetsToLineNumbers[ilOffset] = line;
                }
            }
        }
    }
}
