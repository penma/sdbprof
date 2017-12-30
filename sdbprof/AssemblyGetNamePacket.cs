using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class AssemblyGetNameRequest : IRequestPacket
    {
        public UInt32 assemblyId;

        public AssemblyGetNameRequest(UInt32 assemblyId)
        {
            this.assemblyId = assemblyId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.ASSEMBLY, (byte)CmdAssembly.GET_NAME, PackUnpack.Pack32BE(assemblyId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new AssemblyGetNameReply(replyFrame, assemblyId);
        }
    }

    public class AssemblyGetNameReply : IReplyPacket
    {
        public UInt32 assemblyId;
        public String assemblyName;

        public AssemblyGetNameReply(ReplyFrame replyFrame, UInt32 assemblyId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("AssemblyGetNameReply(" + assemblyId + ") error code " + replyFrame.errorCode);
            }
            this.assemblyId = assemblyId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                assemblyName = ms.ReadUTF8String();
            }
        }
    }
}
