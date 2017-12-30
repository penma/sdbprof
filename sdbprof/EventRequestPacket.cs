using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class EventRequestSetRequest : IRequestPacket
    {
        public EventKind eventKind;
        public SuspendPolicy suspendPolicy;

        public EventRequestSetRequest(EventKind eventKind, SuspendPolicy suspendPolicy /* params Modifier[] modifiers  TODO */)
        {
            this.eventKind = eventKind;
            this.suspendPolicy = suspendPolicy;
        }

        public RequestFrame MakeRequestFrame()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)eventKind);
                ms.WriteByte((byte)suspendPolicy);
                ms.WriteByte(0); // modifier count - TODO
                return new RequestFrame(CommandSet.EVENT_REQUEST, (byte)CmdEventRequest.SET, ms.ToArray());
            }
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new EventRequestSetReply(replyFrame);
        }
    }

    public class EventRequestSetReply : IReplyPacket
    {
        public UInt32 eventRequestId;

        public EventRequestSetReply(ReplyFrame replyFrame)
        {
            if (replyFrame.errorCode != ErrorCode.NONE)
            {
                Console.WriteLine("EventSetReply(...) error code " + replyFrame.errorCode);
            }
            using (MemoryStream ms = new MemoryStream(replyFrame.extraData, false))
            {
                this.eventRequestId = ms.Read32BE();
            }
        }

        public override string ToString()
        {
            return "Event request ID " + eventRequestId;
        }
    }

    public class EventRequestClearRequest : IRequestPacket
    {
        public EventKind eventKind;
        public UInt32 eventRequestId;

        public EventRequestClearRequest(EventKind eventKind, UInt32 eventRequestId)
        {
            this.eventKind = eventKind;
            this.eventRequestId = eventRequestId;
        }

        public RequestFrame MakeRequestFrame()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)eventKind);
                ms.Write32BE(eventRequestId);
                return new RequestFrame(CommandSet.EVENT_REQUEST, (byte)CmdEventRequest.CLEAR, ms.ToArray());
            }
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new EmptyReply(replyFrame);
        }
    }
}
