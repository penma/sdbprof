
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
        public static StreamWriter profileStream = null;
        
        private static void ReopenProfileStream()
        {
            if (profileStream != null)
            {
                profileStream.Close();
            }
            profileStream = new StreamWriter("callgrind.out"); /* XXX error handling, overwrite, etc. */
            profileStream.Write("# callgrind format\nevents: hit\n\n");
            Console.WriteLine("[+] Profile file callgrind.out opened");
        }
        
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

        private static ThreadFrame[] GetThreadStackframe(UInt32 threadId)
        {
            sdb.SendPacketToStreamSync(new VMSuspendRequest());
            ThreadGetFrameInfoReply frameinfo = sdb.SendPacketToStreamSync(new ThreadGetFrameInfoRequest(threadId)) as ThreadGetFrameInfoReply;
            sdb.SendPacketToStreamSync(new VMResumeRequest());
            return frameinfo.frames;
        }

        private static String GetThreadName(UInt32 threadId)
        {
            ThreadGetNameReply nr = sdb.SendPacketToStreamSync(new ThreadGetNameRequest(threadId)) as ThreadGetNameReply;
            return nr.threadName;
        }

        /* Thread ID list */
        private static List<UInt32> threadIds = new List<uint>();
        private static void UpdateThreadIDList()
        {
            VMAllThreadsReply re = sdb.SendPacketToStreamSync(new VMAllThreadsRequest()) as VMAllThreadsReply;
            threadIds.Clear();
            threadIds.AddRange(re.threadObjectIds);
            threadIds.Sort();
        }

        private static void ShowAssemblies()
        {
            Console.WriteLine("\nLoaded assemblies:");
            UInt32 rootDom = (sdb.SendPacketToStreamSync(new DomGetRootRequest()) as DomGetRootReply).domainId;
            DomGetAssembliesReply reAsm = sdb.SendPacketToStreamSync(new DomGetAssembliesRequest(rootDom)) as DomGetAssembliesReply;
            foreach (UInt32 assemblyId in reAsm.assemblyIds)
            {
                AssemblyInfo a = AssemblyInfo.GetOrQueryAssemblyInfo(assemblyId);
                Console.WriteLine("    {0,5} {1}\n          {2}", assemblyId, a.name, a.location);
            }
        }

        private static void ShowThreadList()
        {
            Console.WriteLine("[+] Running threads:");
            sdb.SendPacketToStreamSync(new VMSuspendRequest());
            foreach (UInt32 victim in threadIds)
            {
                ThreadFrame[] frames = GetThreadStackframe(victim);
                String curMethod = "(no frame info)";
                if (frames.Length > 0)
                {
                    MethodInfo mi = MethodInfo.GetOrQueryMethodInfo(frames[0].methodId);
                    curMethod = mi.ToString();
                }
                Console.WriteLine("Thread ID {0} \"{1}\" executing {2}", victim, GetThreadName(victim), curMethod);
            }
            sdb.SendPacketToStreamSync(new VMResumeRequest());
        }

        private static void ShowThreadStacktrace(UInt32 victim)
        {
            ThreadFrame[] frames = GetThreadStackframe(victim);
            Console.WriteLine("Thread ID {0} \"{1}\" has {2} frames:", victim, GetThreadName(victim), frames.Length);
            for (int i = 0; i < frames.Length; i++)
            {
                MethodInfo mi = MethodInfo.GetOrQueryMethodInfo(frames[i].methodId);
                Console.WriteLine("    {0} frameId {1} methodId {2} ({3}) ilOffset {4:X} flags {5}",
                    i, frames[i].frameId, frames[i].methodId, mi, frames[i].ilOffset, frames[i].flags);
            }
        }

        private static UInt32 currentThread;


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

            ReopenProfileStream();

            UpdateThreadIDList();
            ShowThreadList();

            bool recording = false;
            int sampleInterval = 20;

            while (true)
            {
                sdb.ProcessReplies();

                if (recording)
                {
                    /* Dump stacktrace of currently traced thread.
                     * VM is suspended by GetThreadStackframe.
                     * it'd be nicer to suspend just one frame at a time, but that
                     * isn't supported.
                     * Then, find out which threads are running (might have changed
                     * since last time - TODO: There might be an event listener for that)
                     */

                    ThreadFrame[] frames = GetThreadStackframe(currentThread);

                    if (frames.Length > 0)
                    {
                        MethodInfo currentMethod = MethodInfo.GetOrQueryMethodInfo(frames[0].methodId);
                        Program.profileStream.Write("fl={0}\nfn={1}\n{2} 1\n",
                            currentMethod.GoodFilename,
                            currentMethod.GoodName,
                            currentMethod.LineNumber
                            );
                        /* Dump all callers as well */
                        for (int i = 1; i < frames.Length; i++)
                        {
                            ThreadFrame caller = frames[i];
                            ThreadFrame callee = frames[i - 1];
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
                }

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Spacebar:
                            recording = !recording;
                            String act = recording ? "started" : "stopped";
                            profileStream.Flush();
                            Console.WriteLine("[+] Recording of thread {0} \"{1}\" {2}",
                                currentThread, GetThreadName(currentThread), act);
                            break;
                        case ConsoleKey.A:
                            ShowAssemblies();
                            break;
                        case ConsoleKey.L:
                            UpdateThreadIDList();
                            ShowThreadList();
                            break;
                        case ConsoleKey.R:
                            ReopenProfileStream();
                            break;
                        case ConsoleKey.S:
                            ShowThreadStacktrace(currentThread);
                            break;
                        case ConsoleKey.DownArrow:
                            int newIndex = threadIds.IndexOf(currentThread) + 1;
                            currentThread = threadIds[newIndex >= threadIds.Count ? 0 : newIndex];
                            Console.WriteLine("[+] Selected thread {0} \"{1}\"", currentThread, GetThreadName(currentThread));
                            break;
                        case ConsoleKey.UpArrow:
                            newIndex = threadIds.IndexOf(currentThread) - 1;
                            currentThread = threadIds[newIndex < 0 ? threadIds.Count - 1 : newIndex];
                            Console.WriteLine("[+] Selected thread {0} \"{1}\"", currentThread, GetThreadName(currentThread));
                            break;
                        case ConsoleKey.LeftArrow:
                            sampleInterval = Math.Max(sampleInterval - 1, 0);
                            Console.WriteLine("Sample interval set to {0} ms", sampleInterval);
                            break;
                        case ConsoleKey.RightArrow:
                            sampleInterval++;
                            Console.WriteLine("Sample interval set to {0} ms", sampleInterval);
                            break;
                        case ConsoleKey.Q:
                        case ConsoleKey.Escape:
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

                            profileStream.Flush();

                            /* dispose VM etc. and exit (TODO) */
                            sdb.SendPacketToStream(new VMDisposeRequest(), (reply, data) => { });

                            return;
                        default:
                            Console.WriteLine("[+] Available keys:");
                            Console.WriteLine("    space        Enable/disable tracing");
                            Console.WriteLine("    a            Show loaded assemblies");
                            Console.WriteLine("    l            Show (and update) the list of running threads");
                            Console.WriteLine("    r            Restart profile file");
                            Console.WriteLine("    s            Show current thread stacktrace");
                            Console.WriteLine("    up/down      Cycle through threads");
                            Console.WriteLine("    left/right   Set sample interval");
                            Console.WriteLine("    q/esc        Shutdown");
                            Console.WriteLine("Status: {0}recording, selected thread {1}, sample interval {2} ms", recording ? "" : "not ", currentThread, sampleInterval);
                            break;
                    }
                }

                Thread.Sleep(sampleInterval);
            }
        }
    }
}
