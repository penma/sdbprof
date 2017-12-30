
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace sdbprof
{
    class Program
    {
        public static SDB sdb;
        public static StreamWriter profileStream;
        
        
        private static List<EventRequestSetReply> registeredEvents = new List<EventRequestSetReply>();
        private static void registerEvent(EventRequestSetRequest req)
        {
            Console.WriteLine("Registering " + req);
            EventRequestSetReply re = sdb.SendPacketToStreamSync(req) as EventRequestSetReply;
            registeredEvents.Add(re);
            Console.WriteLine("Registered event request id {0}", re.eventRequestId);
        }

        private static Dictionary<UInt32, InferredThreadCallstack> callstacks = new Dictionary<uint, InferredThreadCallstack>();

        private static void RecordEvent(DebugEvent e)
        {
            if (!registeredEvents.Any((re) => re.eventRequestId == e.requestId)) /* XXX also test eventKind */
            {
                Console.WriteLine("[-] Received spurious event: " + e);
                return;
            }

            if (e.eventKind == EventKind.METHOD_ENTRY || e.eventKind == EventKind.METHOD_EXIT)
            {
                if (!callstacks.ContainsKey(e.threadId))
                {
                    callstacks[e.threadId] = new InferredThreadCallstack();
                }
                InferredThreadCallstack cs = callstacks[e.threadId];

                if (e.eventKind == EventKind.METHOD_ENTRY)
                {
                    cs.HandleEntry(e.methodId, e.timestamp);
                }
                else
                {
                    cs.HandleExit(e.methodId, e.timestamp);
                }
            }
        }

        public static void Main(string[] args)
        {
            // XXX Dispose of any old debug sessions
            Console.WriteLine("Resetting debuggee");
            SDB disposeSDB = new SDB("127.0.0.1", 55555);
            disposeSDB.SendPacketToStream(new VMDisposeRequest(), (reply, data) => { });
            disposeSDB.ProcessReplies();
            disposeSDB.stream.Close();
            disposeSDB = null;

            Console.WriteLine("Now doing the real thing");

            sdb = new SDB("127.0.0.1", 55555);

            profileStream = new StreamWriter("callgrind.out"); /* XXX error handling, overwrite, etc. */
            profileStream.Write("# callgrind format\nevent:us:microseconds\nevents: us\n\n");
            Console.WriteLine("[+] Profile file callgrind.out opened");

            // Only trace some
            List<EventRequestModifier> modifiers = new List<EventRequestModifier>();

            Console.WriteLine("[+] Loaded assemblies:");
            UInt32 rootDom = (sdb.SendPacketToStreamSync(new DomGetRootRequest()) as DomGetRootReply).domainId;
            DomGetAssembliesReply reAsm = sdb.SendPacketToStreamSync(new DomGetAssembliesRequest(rootDom)) as DomGetAssembliesReply;
            foreach (UInt32 assemblyId in reAsm.assemblyIds)
            {
                String assemblyName = (sdb.SendPacketToStreamSync(new AssemblyGetNameRequest(assemblyId)) as AssemblyGetNameReply).assemblyName;
                Console.WriteLine("assembly {0} Name {1}", assemblyId, assemblyName);
                /*
                if (assemblyName.StartsWith("MeshInfo"))
                {
                    modifiers.Add(EventRequestModifier.OnlyAssemblies(assemblyId));
                }
                */
            }

            sdb.OnEvent(RecordEvent);
            Console.WriteLine("[*] Setting up event requests");
            registerEvent(new EventRequestSetRequest(EventKind.METHOD_ENTRY, SuspendPolicy.NONE, modifiers.ToArray()));
            registerEvent(new EventRequestSetRequest(EventKind.METHOD_EXIT, SuspendPolicy.NONE, modifiers.ToArray()));
            Console.WriteLine("[+] Event requests have been set up");

            while (true) // XXX while notTerminated or so
            {
                sdb.ProcessReplies();
                CheckTermination();
            }
        }

        /* Check if the user wants to stop profiling. If so, clean things up and let the event queue empty */
        public static void CheckTermination()
        {
            if (Console.KeyAvailable)
            {
                Console.WriteLine("[+] Termination requested");

                /* unsubscribe from events etc. */
                Console.WriteLine("[*] Removing all event requests");
                foreach (EventRequestSetReply re in registeredEvents)
                {
                    sdb.SendPacketToStreamSync(new EventRequestClearRequest(re));
                }
                registeredEvents.Clear();
                Console.WriteLine("[+] All event requests removed");

                /* TODO must still process events and find out about methodInfo etc. */

                /* dispose VM etc. and exit (TODO) */
                sdb.SendPacketToStream(new VMDisposeRequest(), (reply, data) => { });
            }
        }
    }
}
