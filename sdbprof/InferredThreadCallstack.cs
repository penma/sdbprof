
using System;
using System.Collections.Generic;
using System.Linq;

namespace sdbprof
{
    struct CalleeInfo
    {
        public UInt32 methodId;
        public long time;

        public CalleeInfo(UInt32 methodId, long time)
        {
            this.methodId = methodId;
            this.time = time;
        }
    }
    class Callframe
    {
        public UInt32 methodId;
        public long startTime;
        public Queue<CalleeInfo> calleeInfo = new Queue<CalleeInfo>();

        public Callframe(uint methodId, long startTime)
        {
            this.methodId = methodId;
            this.startTime = startTime;
        }

        public void AddCalleeInfo(UInt32 methodId, long calleeTime)
        {
            calleeInfo.Enqueue(new CalleeInfo(methodId, calleeTime));
        }
    }

    class InferredThreadCallstack
    {
        private Stack<Callframe> frames = new Stack<Callframe>();

        public void HandleEntry(UInt32 methodId, long startTime)
        {
            frames.Push(new Callframe(methodId, startTime));
            /* TODO: If we want to record the actual call origin, here is the time to query the stack for the precise caller location */
        }

        public void HandleExit(UInt32 methodId, long endTime)
        {
            if (frames.Count == 0)
            {
                Console.WriteLine("[*] Method " + methodId + " returned, but no entry recorded. Maybe it was running already when profiling was started.");
                return;
            }

            Callframe currentFrame = frames.Pop();

            if (currentFrame.methodId != methodId)
            {
                Console.WriteLine("[-] Expected return from " + currentFrame.methodId + " but actually returned from " + methodId);
                return;
            }

            long ourTimeTotal = endTime - currentFrame.startTime;

            /* If we have a known caller, tell it about our execution time, so it knows its self costs */
            if (frames.Count > 0)
            {
                frames.Peek().AddCalleeInfo(methodId, ourTimeTotal);
            }
            
            /* Now write information for this frame
             * Note that the children's stats have been written already, we
             * just need to record them for "how often did *we* call *them*
             */
            MethodInfo currentMethod = MethodInfo.GetOrQueryMethodInfo(methodId);
            Program.profileStream.Write("fl={0}\nfn={1}\n{2} {3}\n",
                currentMethod.GoodFilename,
                currentMethod.GoodName,
                currentMethod.LineNumber,
                ourTimeTotal - currentFrame.calleeInfo.Sum((callee) => callee.time)
                );
            foreach (CalleeInfo ci in currentFrame.calleeInfo)
            {
                MethodInfo calleeMethod = MethodInfo.GetOrQueryMethodInfo(ci.methodId);
                Program.profileStream.Write("cfi={0}\ncfn={1}\ncalls=1 {2}\n{3} {4}\n",
                    calleeMethod.GoodFilename,
                    calleeMethod.GoodName,
                    calleeMethod.LineNumber,
                    currentMethod.LineNumber,
                    ci.time
                    );
            }
            Program.profileStream.WriteLine();

            Program.profileStream.Flush(); // XXX
        }
    }
}
