using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class TypeGetInfoRequest : IRequestPacket
    {
        public UInt32 typeId;

        public TypeGetInfoRequest(UInt32 typeId)
        {
            this.typeId = typeId;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.TYPE, (byte)CmdType.GET_INFO, PackUnpack.Pack32BE(typeId));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new TypeGetInfoReply(replyFrame, typeId);
        }
    }

    public class TypeGetInfoReply : IReplyPacket
    {
        public UInt32 typeId;

        public String Namespace;
        public String Classname;
        public String FullName;
        public UInt32 AssemblyId;
        public UInt32 ModuleId;
        public UInt32 ParentTypeId;
        public UInt32 UnderlyingTypeId;
        public UInt32 TypeToken;
        public byte TypeRank;
        public UInt32 TypeFlags;
        public byte ByvalFlags;

        public TypeGetInfoReply(ReplyFrame replyFrame, UInt32 typeId)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("TypeGetInfoReply(" + typeId + ") error code " + replyFrame.errorCode);
            }
            this.typeId = typeId;
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                this.Namespace = ms.ReadUTF8String();
                this.Classname = ms.ReadUTF8String();
                this.FullName = ms.ReadUTF8String();
                this.AssemblyId = ms.Read32BE();
                this.ModuleId = ms.Read32BE();
                this.ParentTypeId = ms.Read32BE();
                this.UnderlyingTypeId = ms.Read32BE();
                this.TypeToken = ms.Read32BE();
                this.TypeRank = (byte)ms.ReadByte();
                this.TypeFlags = ms.Read32BE();
                this.ByvalFlags = (byte)ms.ReadByte();
                /* TODO: Nested typeIds */
            }
        }
    }
}
