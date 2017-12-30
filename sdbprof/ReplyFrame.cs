using System;
using System.IO;

namespace sdbprof
{
    public class ReplyFrame : Frame
    {
        public ErrorCode errorCode;
        
        public override String ToString()
        {
            return String.Format("Reply to id={0} flags={4:X2} errorcode={1} + {2} bytes of data <{3}>",
                id, errorCode.ToString(),
                extraData.Length, SDB.DumpByteArray(extraData),
                flags
                );
        }
    }
}