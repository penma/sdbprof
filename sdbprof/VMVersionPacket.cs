using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class VMVersionRequest : IRequestPacket
    {
        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame(CommandSet.VM, (byte) CmdVM.VERSION);
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new VMVersionReply(replyFrame);
        }
    }

    public class VMVersionReply : IReplyPacket
    {
        public string vmInfo;
        public UInt32 major;
        public UInt32 minor;

        public VMVersionReply(ReplyFrame replyFrame) {
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                this.vmInfo = ms.ReadUTF8String();
                this.major = ms.Read32BE();
                this.minor = ms.Read32BE();
            }
        }

        public override string ToString()
        {
            return String.Format("VM version info: {0} ({1}.{2})", vmInfo, major, minor);
        }
    }
}
