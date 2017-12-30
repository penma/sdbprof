using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdbprof
{
    public class EventRequestModifier
    {
        public ModifierKind kind;
        public UInt32[] assemblyIds;
        // TODO: Add more

        public static EventRequestModifier OnlyAssemblies(params UInt32[] assemblyIds)
        {
            EventRequestModifier m = new EventRequestModifier();
            m.kind = ModifierKind.ASSEMBLY_ONLY;
            m.assemblyIds = assemblyIds;
            return m;
        }

        public void ToStream(Stream ms)
        {
            ms.WriteByte((byte)kind);
            switch (kind)
            {
                case ModifierKind.ASSEMBLY_ONLY:
                    ms.Write32BE((UInt32) assemblyIds.Length);
                    foreach (UInt32 aid in assemblyIds)
                    {
                        ms.Write32BE(aid);
                    }
                    break;
                default:
                    throw new NotImplementedException("CUrrently only assembly id modifier kind supported");
            }
        }
    }

    public class EventRequestSetRequest : IRequestPacket
    {
        public EventKind eventKind;
        public SuspendPolicy suspendPolicy;
        public EventRequestModifier[] modifiers;

        public EventRequestSetRequest(EventKind eventKind, SuspendPolicy suspendPolicy, params EventRequestModifier[] modifiers)
        {
            this.eventKind = eventKind;
            this.suspendPolicy = suspendPolicy;
            this.modifiers = modifiers;
        }

        public RequestFrame MakeRequestFrame()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)eventKind);
                ms.WriteByte((byte)suspendPolicy);
                ms.WriteByte((byte) modifiers.Length);
                foreach (EventRequestModifier modifier in modifiers)
                {
                    modifier.ToStream(ms);
                }

                return new RequestFrame(CommandSet.EVENT_REQUEST, (byte)CmdEventRequest.SET, ms.ToArray());
            }
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            return new EventRequestSetReply(replyFrame, eventKind);
        }
    }

    public class EventRequestSetReply : IReplyPacket
    {
        public UInt32 eventRequestId;
        public EventKind eventKind;

        public EventRequestSetReply(ReplyFrame replyFrame, EventKind eventKind)
        {
            this.eventKind = eventKind;
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
            return "Event kind " + eventKind + " request ID " + eventRequestId;
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

        public EventRequestClearRequest(EventRequestSetReply reply)
        {
            this.eventKind = reply.eventKind;
            this.eventRequestId = reply.eventRequestId;
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
