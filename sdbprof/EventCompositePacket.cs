using System;
using System.Text;

namespace sdbprof
{
    internal class EventCompositePacket : RequestPacket
    {
        public byte suspendPolicy;
        public DebugEvent[] events;

        public EventCompositePacket(uint id, byte flags, byte[] extraData)
        {
            this.id = id;
            this.flags = flags;
            this.extraData = extraData;

            this.suspendPolicy = extraData[0];
            int nEvents = (int) Unpack.UInt32(extraData, 1);
            this.events = new DebugEvent[nEvents];

            int offset = 5;
            for (int i = 0; i < nEvents; i++)
            {
                this.events[i] = new DebugEvent(extraData, ref offset);
            }
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
    }

    internal class DebugEvent
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

        public DebugEvent(byte[] extraData, ref int offset)
        {
            eventKind = (EventKind) extraData[offset + 0];
            requestId = Unpack.UInt32(extraData, offset + 1);
            threadId = Unpack.UInt32(extraData, offset + 5);
            offset += 9;

            switch (eventKind)
            {
                case EventKind.THREAD_START:
                case EventKind.THREAD_DEATH:
                    break;
                case EventKind.APPDOMAIN_CREATE:
                case EventKind.APPDOMAIN_UNLOAD:
                case EventKind.VM_START:
                    domainId = Unpack.UInt32(extraData, offset);
                    offset += 4;
                    break;
                case EventKind.METHOD_ENTRY:
                case EventKind.METHOD_EXIT:
                    methodId = Unpack.UInt32(extraData, offset);
                    offset += 4;
                    break;
                case EventKind.ASSEMBLY_LOAD:
                case EventKind.ASSEMBLY_UNLOAD:
                    assemblyId = Unpack.UInt32(extraData, offset);
                    offset += 4;
                    break;
                case EventKind.TYPE_LOAD:
                    typeId = Unpack.UInt32(extraData, offset);
                    offset += 4;
                    break;
                case EventKind.BREAKPOINT:
                case EventKind.STEP:
                    methodId = Unpack.UInt32(extraData, offset);
                    ilOffset = Unpack.UInt64(extraData, offset + 4);
                    offset += 12;
                    break;
                case EventKind.VM_DEATH:
                    /* TODO: may add an int32 in some protocol versions */
                    break;
                case EventKind.EXCEPTION:
                    exceptionId = Unpack.UInt32(extraData, offset);
                    offset += 4;
                    break;
                case EventKind.USER_BREAK:
                case EventKind.KEEPALIVE:
                    break;
                case EventKind.USER_LOG:
                    logLevel = Unpack.UInt32(extraData, offset);
                    offset += 4;
                    logCategory = Unpack.String(extraData, offset);
                    offset += 4 + logCategory.Length;
                    logMessage = Unpack.String(extraData, offset);
                    offset += 4 + logMessage.Length;
                    break;
                default:
                    throw new Exception("Invalid EventKind " + eventKind + ", couldn't unpack");
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} req={1} thread={2}", eventKind.ToString(), requestId, threadId);
            /* TODO: output more fields whe nappropriate */
            return sb.ToString();
        }
    }
}