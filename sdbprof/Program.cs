
using System;
using System.Collections.Generic;
using System.Text;

namespace sdbprof
{
    class Program
    {
        private static SDB sdb;

        private static int counterUntilVmResume = 1;

        private static void onVMVersion(IReplyPacket replyPacket, object data)
        {
            Console.WriteLine("[+] " + replyPacket.ToString());

            sdb.SendPacketToStream(new VMSuspendRequest(), onVMSuspend); // XXX Somehow we never get a reply on this but it still works?
            sdb.SendPacketToStream(new VMAllThreadsRequest(), onAllThreads);
            counterUntilVmResume = 1;
        }

        private static void onVMSuspend(IReplyPacket replyPacket, object data)
        {
            Console.WriteLine("[+] VM suspended");
        }

        private static void onAllThreads(IReplyPacket replyPacket, object data)
        {
            VMAllThreadsReply threads = replyPacket as VMAllThreadsReply;
            Console.WriteLine("[+] Got thread IDs: " + threads.ToString());

            foreach (UInt32 threadId in threads.threadObjectIds)
            {
                sdb.SendPacketToStream(new ThreadGetNameRequest(threadId), onThreadName, threadId);
                sdb.SendPacketToStream(new ThreadGetFrameInfoRequest(threadId), onThreadFrameinfo, threadId);
                counterUntilVmResume += 2;
            }

            counterUntilVmResume--;
        }

        private static void onThreadName(IReplyPacket replyPacket, object threadId)
        {
            ThreadGetNameReply tgn = replyPacket as ThreadGetNameReply;
            Console.WriteLine(String.Format("Thread {0} \"{1}\"", (UInt32)threadId, tgn.threadName));
            counterUntilVmResume--;
        }

        private static Dictionary<UInt32, String> methodNames = new Dictionary<uint, string>();

        private static void onThreadFrameinfo(IReplyPacket replyPacket, object data)
        {
            ThreadGetFrameInfoReply gfi = replyPacket as ThreadGetFrameInfoReply;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Thread ID {0} has {1} frames:", gfi.threadId, gfi.frames.Length);
            for (int i = 0; i < gfi.frames.Length; i++)
            {
                if (!methodNames.ContainsKey(gfi.frames[i].methodId))
                {
                    MethodGetNameReply mn = sdb.SendPacketToStreamSync(new MethodGetNameRequest(gfi.frames[i].methodId)) as MethodGetNameReply;
                    methodNames[mn.methodId] = mn.methodName;
                }

                sb.AppendFormat("\n[{0}] frameId {1} methodId {2} {3} ilOffset {4:X} flags {5}",
                    i,
                    gfi.frames[i].frameId,
                    gfi.frames[i].methodId,
                    methodNames[gfi.frames[i].methodId],
                    gfi.frames[i].ilOffset,
                    gfi.frames[i].flags);
            }
            Console.WriteLine(sb.ToString());
            counterUntilVmResume--;
        }

        private struct EventRequest
        {
            public UInt32 requestId;
            public EventKind eventKind;
            public EventRequest(UInt32 requestId, EventKind eventKind)
            {
                this.requestId = requestId;
                this.eventKind = eventKind;
            }
        }
        private static List<EventRequest> registeredEvents = new List<EventRequest>();
        private static void registerEvent(EventRequestSetRequest req)
        {
            Console.WriteLine("Registering " + req);
            EventRequestSetReply re = sdb.SendPacketToStreamSync(req) as EventRequestSetReply;
            registeredEvents.Add(new EventRequest(re.eventRequestId, req.eventKind));
        }

        public static void Main(string[] args)
        {
            sdb = new SDB("127.0.0.1", 55555);

            bool vmInfoSent = false;
            bool vmStillSuspended = true;
            while (true)
            {
                sdb.ProcessReplies();

                if (!vmInfoSent)
                {
                    sdb.SendPacketToStream(new VMVersionRequest(), onVMVersion);
                    vmInfoSent = true;
                }
                if (counterUntilVmResume == 0 && vmStillSuspended)
                {
                    Console.WriteLine("[+] All frames dumped, resuming VM now");
                    sdb.SendPacketToStream(new VMResumeRequest(), (reply, data) => { Console.WriteLine("[+] VM resumed"); });
                    vmStillSuspended = false;

                    registerEvent(new EventRequestSetRequest(EventKind.METHOD_ENTRY, SuspendPolicy.NONE));
                    registerEvent(new EventRequestSetRequest(EventKind.METHOD_EXIT, SuspendPolicy.NONE));
                }

                if (Console.KeyAvailable)
                {
                    Console.WriteLine("[*] Removing all event requests");
                    foreach (EventRequest re in registeredEvents)
                    {
                        sdb.SendPacketToStreamSync(new EventRequestClearRequest(re.eventKind, re.requestId));
                    }
                    registeredEvents.Clear();
                    Console.WriteLine("[+] All event requests removed");
                }
            }
        }
    }
}
