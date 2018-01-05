using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class HackprofRequest : IRequestPacket
    {
        public byte subcommand;
        public UInt32 param;

        public HackprofRequest(byte subcommand, UInt32 param)
        {
            this.subcommand = subcommand;
            this.param = param;
        }

        public RequestFrame MakeRequestFrame()
        {
            return new RequestFrame((CommandSet)0xcc, subcommand, PackUnpack.Pack32BE(param));
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new HackprofReply(replyFrame, subcommand, param);
        }
    }

    public class HackprofReply : IReplyPacket
    {
        public HackprofReply(ReplyFrame replyFrame, byte subcommand, UInt32 param)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("HackprofRequest({0},{1}) error code {2}", subcommand, param, replyFrame.errorCode.ToString());
            }
        }
    }
}
