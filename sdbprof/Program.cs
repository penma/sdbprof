
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
            profileStream.Write("# callgrind format\nevents: hit\n\n");
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

            UInt32 SIMULATOR = 0;
            Console.WriteLine("[+] Running threads:");
            sdb.SendPacketToStreamSync(new VMSuspendRequest());
            VMAllThreadsReply initThreads = sdb.SendPacketToStreamSync(new VMAllThreadsRequest()) as VMAllThreadsReply;
            foreach (UInt32 victim in initThreads.threadObjectIds)
            {
                ThreadGetNameReply nr = sdb.SendPacketToStreamSync(new ThreadGetNameRequest(victim)) as ThreadGetNameReply;
                ThreadGetFrameInfoReply frameinfo = sdb.SendPacketToStreamSync(new ThreadGetFrameInfoRequest(victim)) as ThreadGetFrameInfoReply;
                String curMethod = "(no frame info)";
                if (frameinfo.frames.Length > 0)
                {
                    MethodInfo mi = MethodInfo.GetOrQueryMethodInfo(frameinfo.frames[0].methodId);
                    curMethod = mi.ToString();
                }
                Console.WriteLine("Thread ID {0} \"{1}\" executing {2}", victim, nr.threadName, curMethod);

                if (nr.threadName == "Simulation") SIMULATOR = victim;
            }
            sdb.SendPacketToStreamSync(new VMResumeRequest());

#if false
            sdb.OnEvent(RecordEvent);
            Console.WriteLine("[*] Setting up event requests");
            registerEvent(new EventRequestSetRequest(EventKind.METHOD_ENTRY, SuspendPolicy.NONE, modifiers.ToArray()));
            registerEvent(new EventRequestSetRequest(EventKind.METHOD_EXIT, SuspendPolicy.NONE, modifiers.ToArray()));
            Console.WriteLine("[+] Event requests have been set up");
#endif

            while (true) // XXX while notTerminated or so
            {
                sdb.ProcessReplies();
                CheckTermination();

                /* Dump stacktrace of threads. Start by suspending the VM first.
                 * it'd be nicer to suspend just one frame at a time, but that
                 * isn't supported.
                 * Then, find out which threads are running (might have changed
                 * since last time - TODO: There might be an event listener for that)
                 */
                sdb.SendPacketToStreamSync(new VMSuspendRequest());
                // VMAllThreadsReply threads = sdb.SendPacketToStreamSync(new VMAllThreadsRequest()) as VMAllThreadsReply;

                // foreach (UInt32 victim in threads.threadObjectIds)
                UInt32 victim = SIMULATOR;
                ThreadGetFrameInfoReply frameinfo = sdb.SendPacketToStreamSync(new ThreadGetFrameInfoRequest(victim)) as ThreadGetFrameInfoReply;
                sdb.SendPacketToStreamSync(new VMResumeRequest());

                if (frameinfo.frames.Length > 0)
                {
                    MethodInfo currentMethod = MethodInfo.GetOrQueryMethodInfo(frameinfo.frames[0].methodId);
                    Program.profileStream.Write("fl={0}\nfn={1}\n{2} 1\n",
                        currentMethod.GoodFilename,
                        currentMethod.GoodName,
                        currentMethod.LineNumber
                        );
                    /* Dump all callers as well */
                    for (int i = 1; i < frameinfo.frames.Length; i++)
                    {
                        ThreadGetFrameInfoReply.Frame caller = frameinfo.frames[i];
                        ThreadGetFrameInfoReply.Frame callee = frameinfo.frames[i - 1];
                        MethodInfo calleeMethod = MethodInfo.GetOrQueryMethodInfo(callee.methodId);
                        MethodInfo callerMethod = MethodInfo.GetOrQueryMethodInfo(caller.methodId);
                        Program.profileStream.Write("fl={3}\nfn={4}\n{5} 0\ncfi={0}\ncfn={1}\ncalls=1 {2}\n{5} 1\n",
                            calleeMethod.GoodFilename,
                            calleeMethod.GoodName,
                            calleeMethod.LineNumber,
                            callerMethod.GoodFilename,
                            callerMethod.GoodName,
                            callerMethod.LineNumber
                            );
                    }
                }

                Thread.Sleep(2);
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
                profileStream.Flush();
            }
        }
    }
}
