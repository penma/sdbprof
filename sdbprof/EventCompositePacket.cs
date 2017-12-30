using System;
using System.IO;
using System.Text;

namespace sdbprof
{
    public class EventCompositePacket : IRequestPacket
    {
        public SuspendPolicy suspendPolicy;
        public DebugEvent[] events;

        public static EventCompositePacket DecodeFrame(RequestFrame requestFrame)
        {
            EventCompositePacket ecp = new EventCompositePacket();

            using (MemoryStream ms = new MemoryStream(requestFrame.extraData, false))
            {
                ecp.suspendPolicy = (SuspendPolicy)ms.ReadByte();
                UInt32 len = ms.Read32BE();
                ecp.events = new DebugEvent[len];
                for (int i = 0; i < len; i++)
                {
                    ecp.events[i] = DebugEvent.FromStream(ms);
                }
            }
            return ecp;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("CompositeEvent<");
            for (int i = 0; i < events.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(events[i].ToString());
            }
            sb.Append('>');
            return sb.ToString();
        }

        public RequestFrame MakeRequestFrame()
        {
            throw new NotImplementedException();
        }

        public IReplyPacket DecodeReplyFrame(ReplyFrame replyFrame)
        {
            throw new NotImplementedException();
        }
    }

    public class DebugEvent
    {
        public EventKind eventKind;
        public UInt32 requestId;
        public UInt32 threadId;
        public UInt32 domainId;
        public UInt32 methodId;
        public UInt32 assemblyId;
        public UInt32 typeId;
        public UInt64 ilOffset;
        public UInt32 exceptionId;
        public UInt32 logLevel;
        public byte[] logCategory;
        public byte[] logMessage;

        public static DebugEvent FromStream(Stream ms)
        {
            DebugEvent e = new DebugEvent();

            e.eventKind = (EventKind)ms.ReadByte();
            e.requestId = ms.Read32BE();
            e.threadId = ms.Read32BE();

            switch (e.eventKind)
            {
                case EventKind.THREAD_START:
                case EventKind.THREAD_DEATH:
                    break;
                case EventKind.APPDOMAIN_CREATE:
                case EventKind.APPDOMAIN_UNLOAD:
                case EventKind.VM_START:
                    e.domainId = ms.Read32BE();
                    break;
                case EventKind.METHOD_ENTRY:
                case EventKind.METHOD_EXIT:
                    e.methodId = ms.Read32BE();
                    break;
                case EventKind.ASSEMBLY_LOAD:
                case EventKind.ASSEMBLY_UNLOAD:
                    e.assemblyId = ms.Read32BE();
                    break;
                case EventKind.TYPE_LOAD:
                    e.typeId = ms.Read32BE();
                    break;
                case EventKind.BREAKPOINT:
                case EventKind.STEP:
                    e.methodId = ms.Read32BE();
                    e.ilOffset = ms.Read64BE();
                    break;
                case EventKind.VM_DEATH:
                    /* TODO: may add an int32 in some protocol versions */
                    break;
                case EventKind.EXCEPTION:
                    e.exceptionId = ms.Read32BE();
                    break;
                case EventKind.USER_BREAK:
                case EventKind.KEEPALIVE:
                    break;
                case EventKind.USER_LOG:
                    e.logLevel = ms.Read32BE();
                    e.logCategory = ms.ReadBytebuf();
                    e.logMessage = ms.ReadBytebuf();
                    break;
                default:
                    throw new Exception("Invalid EventKind " + e.eventKind + ", couldn't unpack");
            }

            return e;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} req={1} thread={2}", eventKind.ToString(), requestId, threadId);
            if (eventKind == EventKind.METHOD_ENTRY || eventKind == EventKind.METHOD_EXIT)
            {
                sb.AppendFormat(" methodId={0}", methodId);
            }
            /* TODO: output more fields whe nappropriate */
            return sb.ToString();
        }
    }
}