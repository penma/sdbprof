using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class AssemblyGetLocationRequest : IRequestPacket
    {
        public UInt32 assemblyId;

        public AssemblyGetLocationRequest(UInt32 assemblyId)
        {
            this.assemblyId = assemblyId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.ASSEMBLY, (byte)CmdAssembly.GET_LOCATION, PackUnpack.Pack32BE(assemblyId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new AssemblyGetLocationReply(replyFrame, assemblyId);
        }
    }

    public class AssemblyGetLocationReply : IReplyPacket
    {
        public UInt32 assemblyId;
        public String location;

        public AssemblyGetLocationReply(ReplyFrame replyFrame, UInt32 assemblyId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("AssemblyGetLocationReply(" + assemblyId + ") error code " + replyFrame.errorCode);
            }
            this.assemblyId = assemblyId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                location = ms.ReadUTF8String();
            }
        }
    }
}
