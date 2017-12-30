using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sdbprof
{
    class SDB
    {
        private Socket sdb;
        public NetworkStream stream;
        private UInt32 nextUnusedPacketId = 0;

        public SDB(string server, int port)
        {
            sdb = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sdb.Connect(server, port);
            stream = new NetworkStream(sdb);

            Handshake();
        }

        private static byte[] handshakeStr = Encoding.ASCII.GetBytes("DWP-Handshake");

        private void Handshake()
        {
            stream.Write(handshakeStr, 0, handshakeStr.Length);

            byte[] recvbuf = new byte[1024];
            int recvlen = stream.Read(recvbuf, 0, handshakeStr.Length);

            /* TODO: Check successful */

            Console.WriteLine("Received " + recvlen + " bytes: <" + Encoding.ASCII.GetString(recvbuf, 0, recvlen) + ">");
        }

        public static string DumpByteArray(byte[] ba)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ba.Length; i++)
            {
                if (i != 0) sb.Append(' ');
                sb.AppendFormat("{0:X2}", ba[i]);
            }
            return sb.ToString();
        }

        /* Handling events */
        private Action<DebugEvent> onEventAction = null;
        public void OnEvent(Action<DebugEvent> action)
        {
            onEventAction = action;
        }

        /* Handling replies */

        private class OutstandingPacketData
        {
            public RequestFrame requestFrame;
            private IRequestPacket requestPacket;
            private Action<IReplyPacket, object> onReply;
            private object onReplyData;
            public OutstandingPacketData(RequestFrame requestFrame, IRequestPacket requestPacket, Action<IReplyPacket, object> onReply, object onReplyData)
            {
                this.requestFrame = requestFrame;
                this.requestPacket = requestPacket;
                this.onReply = onReply;
                this.onReplyData = onReplyData;
            }
            public void Execute(ReplyFrame replyFrame)
            {
                if (this.onReply != null)
                {
                    this.onReply(requestPacket.DecodeReplyFrame(replyFrame), onReplyData);
                }
            }
        }

        private Dictionary<UInt32, OutstandingPacketData> outstanding = new Dictionary<UInt32, OutstandingPacketData>();

        /* Postponed replies in case someone wanted a synchronous answer */
        private Queue<Frame> postponedFrames = new Queue<Frame>();

        /* Events to be processed */
        private Queue<RequestFrame> eventFrameQueue = new Queue<RequestFrame>();

        public void ProcessReplies()
        {
            while (postponedFrames.Count > 0)
            {
                // Console.WriteLine("(processing postponed frame)");
                ProcessFrame(postponedFrames.Dequeue());
            }

#if false
            while (stream.DataAvailable)
            {
                Frame frame = Frame.ReadFromStream(stream);
                ProcessFrame(frame);
            }
#endif
            /* Read as many frames as possible to reduce possible timestamp
             * error.
             * Event frames are filtered out later: Instead of processing them
             * immediately, "processing" them means putting them into a queue
             * that is occasionally processed.
             */
            int frameCounter = 0;
            while (stream.DataAvailable) // XXX was DataAvailable
            {
                stream.ReadTimeout = 10;
                Frame frame = Frame.ReadFromStream(stream);
                if (frame == null) { break;  }
                postponedFrames.Enqueue(frame);
                frameCounter++;
                if (frameCounter % 100000 == 0)
                {
                    Console.WriteLine("... {0} (most recent frame: {1})", frameCounter, EventCompositePacket.DecodeFrame(frame as RequestFrame));
                    Program.CheckTermination();
                }
                if (frameCounter > 123456)
                {
                    Console.WriteLine("Too many frames, processing some");
                    break;
                }
            }

            if (frameCounter + eventFrameQueue.Count > 0) Console.WriteLine("Recorded {0} frames to be processed ({1} still in event queue)", frameCounter, eventFrameQueue.Count);

            int eventsProcessed = 0;
            while (eventFrameQueue.Count > 0 && eventsProcessed < 1000)
            {
                ProcessEventFrame(eventFrameQueue.Dequeue());
                eventsProcessed++;
            }

            if (eventsProcessed + eventFrameQueue.Count > 0) Console.WriteLine("Also processed {0} events frames ({1} still in event queue)", eventsProcessed, eventFrameQueue.Count);
        }

        private void ProcessFrame(Frame frame)
        {
            //Console.WriteLine("(Received frame: " + frame.ToString() + ")");
            if (frame is ReplyFrame)
            {
                if (outstanding.ContainsKey(frame.id))
                {
                    outstanding[frame.id].Execute(frame as ReplyFrame);
                    outstanding.Remove(frame.id);
                }
                else
                {
                    Console.WriteLine("Reply for unknown request received! " + frame.ToString());
                }
            }
            else
            {
                RequestFrame req = frame as RequestFrame;
                if (req.commandSet == CommandSet.EVENT && req.command == (byte)CmdComposite.COMPOSITE)
                {
                    /* Store frame, process it later */
                    // ENQ eventFrameQueue.Enqueue(req);
                }
                else
                {
                    Console.WriteLine("Unknown request frame received: " + frame.ToString());
                }
            }
        }

        private void ProcessEventFrame(RequestFrame req)
        {
            EventCompositePacket ecp = EventCompositePacket.DecodeFrame(req);
            if (onEventAction != null)
            {
                foreach (DebugEvent e in ecp.events)
                {
                    onEventAction(e);
                }
            }
        }

        /* Send a packet and do something on reply */

        public void SendPacketToStream(IRequestPacket request, Action<IReplyPacket, object> onReply, object onReplyData = null)
        {
            /* Turn into a frame */
            RequestFrame rf = request.MakeRequestFrame();

            /* Give it an unique id and store the action to be executed later */
            rf.id = nextUnusedPacketId;
            outstanding[rf.id] = new OutstandingPacketData(rf, request, onReply, onReplyData);
            nextUnusedPacketId++;

            /* Send and hope for the best */
            // Console.WriteLine("(Sending frame: " + rf.ToString() + ")");
            rf.ToStream(stream);
        }

        public IReplyPacket SendPacketToStreamSync(IRequestPacket request)
        {
            /* Turn into a frame */
            RequestFrame rf = request.MakeRequestFrame();

            /* Give it an unique id and store the action to be executed later */
            rf.id = nextUnusedPacketId;
            nextUnusedPacketId++;

            /* Send and hope for the best */
            // Console.WriteLine("(Sending frame: " + rf.ToString() + ")");
            rf.ToStream(stream);

            while (true)
            {
                Frame frame = Frame.ReadFromStream(stream);
                if (frame.id == rf.id && frame is ReplyFrame)
                {
                    Program.CheckTermination();
                    return request.DecodeReplyFrame(frame as ReplyFrame);
                }

                /* Postpone */
                if (frame is ReplyFrame)
                {
                    Console.WriteLine("Postponing a non-event frame\n  Request: {0}\n  Reply: {1}", outstanding[frame.id].requestFrame, frame);
                    postponedFrames.Enqueue(frame);
                }
                else
                {
                    RequestFrame req = frame as RequestFrame;
                    if (req.commandSet == CommandSet.EVENT && req.command == (byte)CmdComposite.COMPOSITE)
                    {
                        /* Store frame, process it later */
                        // ENQ eventFrameQueue.Enqueue(req);
                    }
                    else
                    {
                        // XXX shouldn't handle it here, but makes no sense right now to enqueue because it won't be processed anyway
                        Console.WriteLine("Unknown request frame received: " + frame.ToString());
                    }
                }
            }
        }
    }
}
